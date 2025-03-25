namespace FileLink.Client.DirectoryNavigation;

/*
    This class will cover the logic that allows the client to navigate the directories they
    have hosted on the server. The problem we ran into was having a reliable navigation method 
    without having prebuilt directory paths to follow inside the client. 
    
    We came up with storing them in a map that maps the file path to all files inside each directory.
    This allows up to update specific directories without having to rewrite the entire client's directory 
    each time, and all without the client having any of this directory tree built on their device. 

    This summary is to get everyone caught up on client navigation. can be deleting later. 
*/

public class DirectoryMap
{
    Dictionary<string, string[]> createClientDirectoryMap()
    {
        Dictionary<string, string[]> map = new Dictionary<string, string[]>();
    
        string filePath = "ClientDirectoryStorage.txt";
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            string[] parts;
            while ((line = reader.ReadLine()) != null)
            {
                parts = line.Split(',');
                map.Add(parts[0], parts.Skip(1).ToArray());
            }
        }
    
        return map;
    }
}

// TODO Function that, on directory click, updates all of the files/directories based on the request 