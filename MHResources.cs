using System;
using System.IO;
using Distance_Func;
using System.Linq;
using wordVecDistance;
using System.Collections.Generic;

namespace MHBot
{
    public class Resource
    {
        public string title;
        public string link;
        public string info;
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
        string[] tag_list;
        int num_resources = 0;

        public InputProcessing(){
            gv.load_from_file(globals.GV_PATH);
            tag_list = File.ReadAllText(globals.TAG_PATH).Split(globals.DELIM);
            tag_list = Array.ConvertAll(tag_list, d => d.ToLower());
            tag_list_emb.load(this.tag_list, gv);
        }
        public void load_resources(string path)
        {
            using (StreamReader sr = File.OpenText(path))
            {

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    this.num_resources++;
                    string[] parts = line.Split('\t');
                    string[] pos_tags = parts[1].Split(globals.DELIM2); //also remove punctuations             
                    List<string> tags = new List<string>();
                    foreach (string word in pos_tags)
                    {
                        if (tag_list.Contains(word))
                        {
                            tags.Add(word);
                        }
                    }
                    all_resources.Add(new Resource() { title = parts[0], tags = tags });
                }
            }
        }
        public List<List<Word_Prob>> GetTags(string[] key_words)
        {
            Console.WriteLine("done loading");
            // string [] key_words=File.ReadAllText(globals.KEY_WORDS_PATH).Split(globals.DELIM);
            // key_words = Array.ConvertAll(key_words, d => d.ToLower());

            key_words_emb.load(key_words, gv);
            return key_words_emb.list_tags(tag_list_emb);

        }

        public List<Resource> GetResources(List<List<Word_Prob>> tag_prob)
        { //Consider changing tag_prob type
            double[] scores = new double[num_resources];

            List<Resource> resources = new List<Resource>();
            for (int i = 0; i < num_resources; i++)
            {
                foreach (List<Word_Prob> j in tag_prob)
                {
                    foreach (Word_Prob k in j)
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

        public void resetCurrEmb(){
            key_words_emb.reset();
        }
    }
}
