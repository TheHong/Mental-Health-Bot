using System;
using System.IO;
using Distance_Func;
using System.Linq;
using wordVecDistance;
using System.Collections.Generic;

namespace MyEchoBot
{
    public class Resource{
        public string title;
        public string link;
        public string info;
        public List <string> tags;

    }

    public class InputProcessing
    {
        Embedding gv=new Embedding();
        Embedding tag_list_emb=new Embedding();
        Embedding key_words_emb=new Embedding();
        public List<Resource> all_resources= new List<Resource>();
        string [] tag_list;
        int num_resources=0;
        public void load_resources(string path){
            
            this.tag_list=File.ReadAllText(globals.TAG_PATH).Split(globals.DELIM);
            this.tag_list = Array.ConvertAll(tag_list, d => d.ToLower());
            
            
            using (StreamReader sr=File.OpenText(path)){
                
                string line;
                while ((line=sr.ReadLine())!=null){
                    this.num_resources++;
                    string[] parts = line.Split('\t');    
                    string[] pos_tags= parts[1].Split(globals.DELIM2); //also remove punctuations             
                    List<string> tags=new List<string>();
                    foreach (string word in pos_tags){
                        if (tag_list.Contains(word)){
                            tags.Add(word);
                        }
                    }
                    all_resources.Add(new Resource(){title=parts[0], tags=tags});
                }
            }
        }
        public List<List<Word_Prob>> GetTags(string[] key_words){
            
            gv.load_from_file(globals.GV_PATH); 

            Console.WriteLine("done loading");            
            // string [] key_words=File.ReadAllText(globals.KEY_WORDS_PATH).Split(globals.DELIM);
            // key_words = Array.ConvertAll(key_words, d => d.ToLower());

            tag_list_emb.load(this.tag_list, gv);
            key_words_emb.load(key_words, gv);
            return key_words_emb.list_tags(tag_list_emb);
            
        }

        public List<Resource> GetResources(List<List<Word_Prob>> tag_prob){ //Consider changing tag_prob type
            double [] scores=new double[num_resources];

            List <Resource> resources= new List<Resource>();
            for (int i=0; i<num_resources; i++){
                foreach(List<Word_Prob> j in tag_prob){
                    foreach(Word_Prob k in j){
                        if (this.all_resources[i].tags.Contains(k.word)){scores [i]+=k.prob;}                                    
                    }                    
                }
            }
            int[] score_indices = scores.Select((r, i) => new { Value = r, Index = i })
                            .OrderBy(t => t.Value)
                            .Select(p => p.Index)
                            .ToArray();

            for (int i=score_indices.Length-1; i>score_indices.Length-globals.NUM_RESOURCES-1; i--){
               
                resources.Add(this.all_resources[score_indices[i]]);
            }
            return resources;
        }
    }
    class Program{
        public static void Main(){
            InputProcessing test= new InputProcessing();
            string [] key_words= new string[]{"feminine"};

            test.load_resources("Data/UofT Mental Health Resources.txt");
            List<List<Word_Prob>> tags=test.GetTags(key_words);
            // foreach (List<Word_Prob> i in tags){
            //     Console.WriteLine("WORD TAG");
            //     foreach (Word_Prob j in i){
            //         j.print();
            //     }
            // }
            // foreach(Resource r in test.all_resources){
            //     Console.WriteLine(r.title);
            //     r.tags.ForEach(Console.Write);
            // }
            List<Resource> resources=test.GetResources(tags);
            foreach (Resource i in resources){
                Console.WriteLine("{0}, ", i.title);
            }
        }
    }
}
