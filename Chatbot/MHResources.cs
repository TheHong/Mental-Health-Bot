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
        public string[] GetTags(string[] keywords){
            return new string[1];
        }

        public Resource[] GetResources(string[] tags){
            return new Resource[1];
        }
    }
}
