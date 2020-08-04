using System;
using Systems.IO;
using Distance_Func;
namespace wordVecDistance{
  char[] DELIM={ ' ', ',', ':', '\t' };
  int MAXTAGS=3;
  class Embedding{//TODO: change some of the types (arrays->list)
    stoi<string, int> dict = new Dictionary<string, int>();
    int num_ex;
    int emb_size;
    string [] itos;
    float [,] vectors;
    public void load(string[] words, Embedding gv){
      itos=Array.Copy(words);
      num_ex=itos.Length;
      emb_size=gv.emb_size;
      vectors=new float [num_ex,emb_size];

      for (int i=0;i<size; i++){
        stoi.Add({itos[i], i});
        vectors[i, 0..emb_size]=gv.vectors[gv.stoi[itos[i]]];

      }

    }
    //used for the one to contain them all
    public void load_from_file(string path){
      using (StreamWriter sr=File.OpenText(path)){
        string s;
        while ((line=sr.ReadLine())!=null){
          string[] words = line.Split(DELIM);
          emb_size= words.Length-1; //except the first string which is the word

        }
      }
    }
    public void list_tags(Embedding vocab){
      LrNorm dist= new LrNorm();
      string[,] tags= new string[num_ex, MAX_TAGS];
      float[] distances= new float[vocab.num_ex];

      foreach(float[] vector in vectors){
        foreach(float[] vocab_vector in vocab.vector){
          dist.Euclidean()
        }
      }
    }

  }
  class Program
  {
    public static void Main(string[] args)
    {
      
    }
  }

}
