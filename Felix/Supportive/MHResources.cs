// Contains classes and methods relevant to storing and processing mental health resources

using System;
using System.IO;
using System.Linq;
using WordVectors;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace MHBot
{
    public class Resource
    {
        // Class defining one single mental health resource that would be fetched to user
        public string title;
        public string subtitle;
        public string info;
        public string link;
        public string imageURL;
        public List<string> tags;
        public ThumbnailCard ToCard() => new ThumbnailCard
        {
            Title = title,
            Subtitle = subtitle,
            Text = info,
            Images = imageURL == "" ? null : new List<CardImage> { new CardImage(imageURL) },
            Buttons = link == "" ? null : new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Go to Website", value: link) },
        };
    }

    public class InputProcessing
    {
        public static int NUM_RESOURCES_FETCH = 3;
        public static string GLOVE_VECTORS_PATH = "Data/new_corpus.csv";
        public static string TAG_PATH = "Data/Tags.txt";
        public static string MHResources_PATH = "Data/UofT Mental Health Resources.txt";
        Embedding gloveVector = new Embedding();
        Embedding tagListEmbedding = new Embedding();
        Embedding keywordsEmbedding = new Embedding();
        public List<Resource> allResources = new List<Resource>();
        string[] tagList;
        int numResources = 0;

        public InputProcessing()
        {
            gloveVector.LoadFromFile(GLOVE_VECTORS_PATH);
            tagList = File.ReadAllText(TAG_PATH).Split(Info.DELIM);
            tagList = Array.ConvertAll(tagList, d => d.ToLower());
            tagListEmbedding.Load(this.tagList, gloveVector);
        }

        public void LoadResources()
        {
            using (StreamReader sr = File.OpenText(MHResources_PATH))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Extracting info of the current line/row
                    string[] parts = line.Split('\t');

                    // Get tags
                    string[] possibleTags = parts[6].ToLower().Split(Info.DELIM); //also remove punctuations             
                    List<string> tags = new List<string>();
                    foreach (string word in possibleTags)
                    {
                        if (tagList.Contains(word))
                        {
                            tags.Add(word);
                        }
                    }

                    // Create and add resource
                    allResources.Add(new Resource()
                    {
                        title = GetPart(parts, 0),
                        subtitle = GetPart(parts, 2) == "" ? $"{GetPart(parts, 1)}" : $"{GetPart(parts, 1)} ({GetPart(parts, 2)})",
                        link = GetPart(parts, 3),
                        info = $"(**{GetPart(parts, 0)}**) {GetPart(parts, 4)}",
                        imageURL = GetPart(parts, 5),
                        tags = tags
                    });
                    this.numResources++;
                }
            }
        }

        private string GetPart(string[] parts, int idx)
        {
            return string.IsNullOrEmpty(parts[idx]) ? "" : parts[idx];
        }

        public List<List<WordProb>> GetTags(string[] key_words)
        {
            Console.WriteLine("done loading");
            // string [] key_words=File.ReadAllText(globals.KEY_WORDS_PATH).Split(globals.DELIM);
            // key_words = Array.ConvertAll(key_words, d => d.ToLower());
            keywordsEmbedding.Load(key_words, gloveVector);
            return keywordsEmbedding.ListTags(tagListEmbedding);
        }

        public List<Resource> GetResources(List<List<WordProb>> tag_prob)
        { //Consider changing tag_prob type
            double[] scores = new double[numResources];
            List<Resource> resources = new List<Resource>();
            for (int i = 0; i < numResources; i++)
            {
                foreach (List<WordProb> j in tag_prob)
                {
                    foreach (WordProb k in j)
                    {
                        if (this.allResources[i].tags.Contains(k.word)) { scores[i] += k.prob; }
                    }
                }
            }
            int[] score_indices = scores.Select((r, i) => new { Value = r, Index = i })
                            .OrderBy(t => t.Value)
                            .Select(p => p.Index)
                            .ToArray();

            for (int i = score_indices.Length - 1; i > score_indices.Length - NUM_RESOURCES_FETCH - 1; i--)
            {
                resources.Add(this.allResources[score_indices[i]]);
            }
            return resources;
        }

        public void resetCurrEmbedding() { keywordsEmbedding.Reset(); }
    }
}
