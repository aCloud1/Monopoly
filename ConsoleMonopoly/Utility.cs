namespace ConsoleMonopoly
{
    public class JsonReader
    {
        public List<string> file_paths;
        public JsonReader()
        {
            file_paths = new List<string>();
        }

        public List<string> list()
        {
            return file_paths = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".json") && s.Count(c => c == '.') == 2)
                    .ToList();
        }

        public List<string> getFilePaths() { return file_paths; }


    }


    public static class Input
    {
        public static int checkStringForInts(string input)
        {
            int int_value;
            if (Int32.TryParse(input, out int_value))
                return int_value;
            else
                return -1;
        }

        public static int askForIntegerInput()
        {
            return Input.checkStringForInts(Console.ReadLine());
        }

        public static string getString()
        {
            string str;
            do
            {
                str = Console.ReadLine();
                if (str != null && str != "")
                    break;
            } while (true);
            return str;
        }
    }
}
