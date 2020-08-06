namespace MyEchoBot
{
    public class Resource
    {
        public string title;
        public string link;
        public string info;
        public string[] tags;

        public string ToStr()
        {
            string[] info = [
                title.ToUpper() + "\n\n",
                info + "\n",
                $"You can find more info at {link}"
            ];
            return string.Join(info);
        }
    }

    public class InputProcessing
    {
        public string[] GetTags(string[] keywords)
        {
            return new string[1];
        }

        public Resource[] GetResources(string[] tags)
        {
            return new Resource[1];
        }
    }
}
