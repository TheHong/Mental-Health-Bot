using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WordVectors
{
    static class globals
    {
        public static string GV_PATH = "Data/new_corpus.csv";
        public static string TAG_PATH = "Data/Tags.txt";

        public static char[] DELIM = { ' ', '\t', '\n', '\r' };
        public static char[] DELIM2 = { ' ', ',', '"', '\t', '\n', '\r' };
        public static int MAX_TAGS = 3;
        public static int NUM_RESOURCES = 3;
        public static void SoftmaxNeg(double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (double)Math.Exp(-values[i]);
            }

            double sum = values.Sum();

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i] / sum;
            }
        }

        public static double Euclidean(List<double> vec1, List<double> vec2)
        {
            double distance = 0;
            for (int i = 0; i < vec1.Count; i++)
            {
                distance += Math.Pow(vec1[i] - vec2[i], 2);
            }
            return Math.Sqrt(distance);
        }
    }
    public class WordProb
    {
        public string word;
        public double prob;
        public string ToStr()
        {
            return $"({this.word}, {this.prob})";
        }
    }
    class Embedding
    {
        // String to index
        public Dictionary<string, int> stoi = new Dictionary<string, int>();
        public int num_ex;
        public int emb_size = -1; //-1 means unassigned
        public List<string> itos = new List<string>();
        public List<List<double>> vectors = new List<List<double>>();
        public void Load(string[] words, Embedding gv)
        {
            this.itos = new List<string>(words); // change type
            this.num_ex = itos.Count;
            this.emb_size = gv.emb_size;
            this.vectors = new List<List<double>>();
            // this.itos.ForEach(Console.WriteLine);
            for (int i = 0; i < this.num_ex; i++)
            {
                this.stoi.Add(this.itos[i], i);
                this.vectors.Add(gv.vectors[gv.stoi[this.itos[i]]]);
            }
        }
        //used for the one to contain them all
        public void LoadFromFile(string path)
        {
            using (StreamReader sr = File.OpenText(path))
            {
                int i = 0;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split(globals.DELIM);
                    if (emb_size == -1)
                    {
                        this.emb_size = parts.Length - 1; //except the first string which is the word
                    }
                    string word = parts[0];
                    this.stoi.Add(word, i);
                    this.itos.Add(word);
                    this.vectors.Add(Array.ConvertAll(parts[1..], double.Parse).ToList());//careful about end
                    i++;
                }
            }
        }
        public List<List<WordProb>> ListTags(Embedding tag_list)
        {//watch out for copying reference vs values
            List<List<WordProb>> allTags = new List<List<WordProb>>();
            double[] distances = new double[tag_list.num_ex];

            foreach (List<double> vector in this.vectors)
            {
                List<WordProb> tags = new List<WordProb>();
                for (int i = 0; i < tag_list.num_ex; i++)
                {
                    distances[i] = (double)globals.Euclidean(vector, tag_list.vectors[i]);
                    // Console.WriteLine("{0}=distance", distances[i]);
                }

                globals.SoftmaxNeg(distances);

                double[] distances_copy = new double[tag_list.num_ex];
                distances.CopyTo(distances_copy, 0);
                Array.Sort(distances);

                Index num_tags = ^(int)Math.Min(globals.MAX_TAGS, Math.Floor(1 / distances.Max()));

                double[] max_prob = distances[num_tags..];
                for (int i = max_prob.Length - 1; i > -1; i--)
                {//put most likely first
                    tags.Add(new WordProb()
                    {
                        word = tag_list.itos[Array.IndexOf(distances_copy, max_prob[i])],
                        prob = max_prob[i]
                    });
                }
                allTags.Add(tags);
            }
            return allTags;
        }

        public void Reset()
        {
            stoi.Clear();
            num_ex = 0;
            emb_size = -1; //-1 means unassigned
            itos.Clear();
            vectors.Clear();
        }
    }
}
