using System;
using Systems.IO;
using Distance_Func;
using System.Linq;
namespace wordVecDistance{
  char[] DELIM={ ' ', ',', ':', '\t' };
  int MAXTAGS=3;
  string GV_PATH=".vector_cache/glove.6B.50d.txt";
  string TAG_PATH="Data/Tags.csv";
  string KEY_WORDS_PATH="Data/Key_words.txt"; //TODO: connect it directly to Luis
  void softmax(float[] values){
    for (int i=0; i<values.Length; i++){
      values[i]=Math.Exp(values[i]);
    }
    sum=values.Sum();
    for (int i=0; i<values.Length; i++){
      values[i]=values[i]/sum;
    }    
  }
  void reciprocal(float[] values){
    for (int i=0; i<values.Length; i++){
      values[i]=1/(values[i]);
    }
  }
  class Embedding{//TODO: change some of the types (arrays->list)
    Dictionary<string, int> stoi = new Dictionary<string, int>();
    int num_ex;
    int emb_size=-1; //-1 means unassigned
    List <string> itos= new List<string>;
    List <float[]> vectors= new List <float[]>();
    public void load(string[] words, Embedding gv){
      itos=Array.Copy(words);
      num_ex=itos.Length;
      emb_size=gv.emb_size;
      vectors=new float [num_ex,emb_size];

      for (int i=0;i<size; i++){
        stoi.Add(itos[i], i);
        vectors[i, 0..emb_size]=gv.vectors[gv.stoi[itos[i]]];
        
      }
    }
    //used for the one to contain them all
    public void load_from_file(string path){
      using (StreamWriter sr=File.OpenText(path)){
        string s;
        int i=0;
        while ((line=sr.ReadLine())!=null){
          string[] parts = line.Split(DELIM);
          if (emb_size==-1){
            emb_size= parts.Length-1; //except the first string which is the word
          }
          word=parts[0];
          stoi.Add(word, i);
          itos.Add(word);
          vectors.Add(Array.ConvertAll(parts[1..]), float.Parse);//carefule about end
        }
      }
    }
    public List <List<string>> list_tags(Embedding tag_list){//watch out for copying reference vs values
      LrNorm dist= new LrNorm();
  
      List <List<string>> all_tags= new List<List<string>>();

      float[] distances= new float[tag_list.num_ex];

      foreach(float[] vector in vectors){
        List<string> tags=new List<string>();

        for(int i=0; i<vocab_vector.num_ex;i++){
          distances[i]=distancesdist.Euclidean(vector, vocab_vector.vectors[i]);
        }
        reciprocal(distances);
        softmax(distances);
        var distances_copy= new float[tag_list.num_ex];
        distances.CopyTo(distances_copy);
        distances.Sort();
        
        Index num_tags= ^(int)Math.Min(MAX_TAGS, Math.Floor(1/distances.Max()));
        max_prob=distances[num_tags..^1];
        for (int i=max_prob.Length-1; i>-1; i--){//put most likely first
          tags.Add(itos[Array.IndexOf(distances_copy, max_prob[i])]);
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
      gv.load_from_file(GV_PATH); 

      string [] tag_list=File.ReadAllText(TAG_PATH).Split(DELIM);
      string [] key_words=File.ReadAllText(KEY_WORDS_PATH).Split(DELIM);

      tag_list_emb.load(tag_list, gv);
      key_words_emb.load(key_words, gv);
      tags=key_words.list_tags(tag_list);
      Console.WriteLine(tags);

      

    }
  }

}
