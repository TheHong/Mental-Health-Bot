using System;
using System.IO;
using System.Linq;
using WordVectors;
using System.Collections.Generic;

namespace MHBot
{
    public class Resource
    {
        public string title;
        public string subtitle;
        public string link;
        public string info;

        public string imageURL;
        public List<string> tags;

        public string ToStr()
        {
            string[] infoList = {
                title.ToUpper() + "\n",
                info + "\n",
                $"You can find more info at {link}",
                "============================"
            };
            return string.Join("", infoList);
        }
    }

    public class InputProcessing
    {
        Embedding gv = new Embedding();
        Embedding tag_list_emb = new Embedding();
        Embedding key_words_emb = new Embedding();
        public List<Resource> all_resources = new List<Resource>();
        string[] tagList;
        int num_resources = 0;

        public InputProcessing()
        {
            gv.LoadFromFile(globals.GV_PATH);
            tagList = File.ReadAllText(globals.TAG_PATH).Split(globals.DELIM);
            tagList = Array.ConvertAll(tagList, d => d.ToLower());
            tag_list_emb.Load(this.tagList, gv);
        }
        public void load_resources(string path)
        {
            using (StreamReader sr = File.OpenText(path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Extracting info of the current line/row
                    string[] parts = line.Split('\t');

                    // Get tags
                    string[] possibleTags = parts[6].ToLower().Split(globals.DELIM); //also remove punctuations             
                    List<string> tags = new List<string>();
                    foreach (string word in possibleTags)
                    {
                        if (tagList.Contains(word))
                        {
                            tags.Add(word);
                        }
                    }

                    // Create and add resource
                    all_resources.Add(new Resource()
                    {
                        title = GetPart(parts, 0),
                        subtitle = GetPart(parts, 2) == ""? $"{GetPart(parts, 1)}" : $"{GetPart(parts, 1)} ({GetPart(parts, 2)})",
                        link = GetPart(parts, 3),
                        info = $"(**{GetPart(parts, 0)}**) {GetPart(parts, 4)}",
                        imageURL = GetPart(parts, 5),
                        tags = tags
                    });

                    this.num_resources++;
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

            key_words_emb.Load(key_words, gv);
            return key_words_emb.ListTags(tag_list_emb);

        }

        public List<Resource> GetResources(List<List<WordProb>> tag_prob)
        { //Consider changing tag_prob type
            double[] scores = new double[num_resources];

            List<Resource> resources = new List<Resource>();
            for (int i = 0; i < num_resources; i++)
            {
                foreach (List<WordProb> j in tag_prob)
                {
                    foreach (WordProb k in j)
                    {
                        if (this.all_resources[i].tags.Contains(k.word)) { scores[i] += k.prob; }
                    }
                }
            }
            int[] score_indices = scores.Select((r, i) => new { Value = r, Index = i })
                            .OrderBy(t => t.Value)
                            .Select(p => p.Index)
                            .ToArray();

            for (int i = score_indices.Length - 1; i > score_indices.Length - globals.NUM_RESOURCES - 1; i--)
            {

                resources.Add(this.all_resources[score_indices[i]]);
            }
            return resources;
        }

        public void resetCurrEmb()
        {
            key_words_emb.Reset();
        }

    }

}
