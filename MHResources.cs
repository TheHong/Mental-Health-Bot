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
        public string[] tags;

    }

    public class InputProcessing
    {
        Resource [] resources= new Resource[]{};

        public List<List<Word_Prob>> GetTags(string[] key_words){
            Embedding gv=new Embedding();
            Embedding tag_list_emb=new Embedding();
            Embedding key_words_emb=new Embedding();
            gv.load_from_file(globals.GV_PATH); 

            Console.WriteLine("done loading");

            string [] tag_list=File.ReadAllText(globals.TAG_PATH).Split(globals.DELIM);
            tag_list = Array.ConvertAll(tag_list, d => d.ToLower());

            // string [] key_words=File.ReadAllText(globals.KEY_WORDS_PATH).Split(globals.DELIM);
            // key_words = Array.ConvertAll(key_words, d => d.ToLower());


            tag_list_emb.load(tag_list, gv);
            key_words_emb.load(key_words, gv);
            return key_words_emb.list_tags(tag_list_emb);
            
        }

        public Resource[] GetResources(string[] tags){
            return new Resource[1];
        }
    }
    class Program{
        public static void Main(){
            InputProcessing test= new InputProcessing();
            string [] key_words= new string[]{"feminine"};
            List<List<Word_Prob>> tags=test.GetTags(key_words);
            foreach (List<Word_Prob> i in tags){
                Console.WriteLine("WORD TAG");
                foreach (Word_Prob j in i){
                    j.print();
                }
            }

        }
    }
}
