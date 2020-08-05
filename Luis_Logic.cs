using System;
using System.IO;
using System.Collections.Generic;
using Distance_Func;
using System.Linq;
namespace wordVecDistance{
  static class globals{
    public static string GV_PATH=".vector_cache/glove.6B.50d.txt";
    public static string TAG_PATH="Data/Tags.txt";
    public static string KEY_WORDS_PATH="Data/Key_words.txt"; //TODO: connect it directly to Luis
    
    public static char[] DELIM={ ' ', '\t', '\n', '\r'};
    public static int MAX_TAGS=3;
    public static void softmax(double[] values){
      for (int i=0; i<values.Length; i++){
        values[i]=(double)Math.Exp(values[i]);
      }
      double sum=values.Sum();
      for (int i=0; i<values.Length; i++){
        values[i]=values[i]/sum;
      }    
    }
    public static void incr_reciprocal(double[] values, double incr){
      for (int i=0; i<values.Length; i++){
        values[i]=1/(values[i]+incr);
      }
    }
    public static double Euclidean(List<double> vec1, List<double> vec2){
      double distance=0;
      for (int i=0; i<vec1.Count; i++){
        distance+= Math.Pow(vec1[i]-vec2[i], 2);
      } 
      return distance;
    }
  }
  class Embedding{//TODO: change some of the types (arrays->list)
    


    Dictionary<string, int> stoi = new Dictionary<string, int>();
    public int num_ex;
    int emb_size=-1; //-1 means unassigned
    List <string> itos= new List<string>();
    List <List<double>> vectors= new List <List<double>>();
    public void load(string[] words, Embedding gv){
      this.itos= new List <string>(words);//change type
      this.num_ex=itos.Count;
      this.emb_size=gv.emb_size;
      this.vectors=new List<List<double>>();
      // this.itos.ForEach(Console.WriteLine);
      for (int i=0;i<this.num_ex; i++){
        this.stoi.Add(this.itos[i], i);
        // Console.WriteLine(this.itos[i]);  
        // Console.WriteLine(gv.stoi[this.itos[i]]);
        // Console.WriteLine(gv.vectors[gv.stoi[this.itos[i]]]);

        this.vectors.Add(gv.vectors[gv.stoi[this.itos[i]]]);
        
      }
    }
    //used for the one to contain them all
    public void load_from_file(string path){
      using (StreamReader sr=File.OpenText(path)){
        int i=0;
        string line;
        while ((line=sr.ReadLine())!=null){
          string[] parts = line.Split(globals.DELIM);
          if (emb_size==-1){
            this.emb_size= parts.Length-1; //except the first string which is the word
          }
          string word=parts[0];
          this.stoi.Add(word, i);
          this.itos.Add(word);      
          this.vectors.Add(Array.ConvertAll(parts[1..], double.Parse).ToList());//careful about end
          i++;
        }
      }
    }
    public List <List<string>> list_tags(Embedding tag_list){//watch out for copying reference vs values
  
      List <List<string>> all_tags= new List<List<string>>();

      double[] distances= new double[tag_list.num_ex];

      foreach(List<double> vector in this.vectors){
        List<string> tags=new List<string>();
        
        for(int i=0; i<tag_list.num_ex;i++){
          distances[i]=(double)globals.Euclidean(vector, tag_list.vectors[i]);
          // Console.WriteLine("{0}=distance", distances[i]);
        }


        globals.incr_reciprocal(distances, 0.01);
        globals.softmax(distances);

        // Array.ForEach(distances, Console.WriteLine);

        double [] distances_copy= new double[tag_list.num_ex];
        distances.CopyTo(distances_copy,0);
        Array.Sort(distances);

        Index num_tags= ^(int)Math.Min(globals.MAX_TAGS, Math.Floor(1/distances.Max()));

        // Console.WriteLine(num_tags);
        // Console.WriteLine(Math.Floor(1/distances.Max()));

        double [] max_prob=distances[num_tags..];
        for (int i=max_prob.Length-1; i>-1; i--){//put most likely first
          tags.Add(tag_list.itos[Array.IndexOf(distances_copy, max_prob[i])]);
        }

        all_tags.Add(tags);

      }
      return all_tags;
    }

  }
  class Program
  {
    public static void Main(string[] args)
    {      
      Embedding gv=new Embedding();
      Embedding tag_list_emb=new Embedding();
      Embedding key_words_emb=new Embedding();
      gv.load_from_file(globals.GV_PATH); 

      Console.WriteLine("done loading");

      string [] tag_list=File.ReadAllText(globals.TAG_PATH).Split(globals.DELIM);
      tag_list = Array.ConvertAll(tag_list, d => d.ToLower());

      string [] key_words=File.ReadAllText(globals.KEY_WORDS_PATH).Split(globals.DELIM);
      key_words = Array.ConvertAll(key_words, d => d.ToLower());


      tag_list_emb.load(tag_list, gv);
      key_words_emb.load(key_words, gv);
      List <List<string>> tags=key_words_emb.list_tags(tag_list_emb);
      Console.WriteLine("end------");
      for (int i=0; i<key_words_emb.num_ex; i++){
        Console.WriteLine("word: {0}", key_words[i]);
        tags[i].ForEach(Console.WriteLine);
      }
      

    }
  }

}
