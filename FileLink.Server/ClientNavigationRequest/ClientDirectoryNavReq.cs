namespace FileLink.Server.ClientNavigationRequest;

/*
    This class, which can be changed or moved wherever i didnt know where you would want it included, will 
    deal will creation of a record of the users entire directory in a format that the client has been designed 
    to handle. It will start in the first function at a 'root' directory, which will probably be set to the path
    to where user directories are stored + the userID (we will test this). 
    
    Continuing the second function. This is a backtracking recursive function that goes all the way into a directory
    until empty. Once there it will record the name (and path leading to) the current directory, along with the names 
    of each file inside the directory. It adds them all to a string which acts as a CSV file. It works very well when 
    demoing it but we will have to tweak based off the servers implementation of directory structure. 
    
    the completeServerDir ends up in this format. 
    
    root/directory3/sub3_1,file2.txt,file3.txt,file1.txt,
    root/directory2/sub2_1,file2.txt,file1.txt,
    root/directory3/sub3_1,file2.txt,file3.txt,file1.txt,
    root/directory3/sub3_3,file2.txt,file1.txt,
    
    See FileLink.Client.DirectoryNavigation to see the other half of this algo
    
*/

public class ClientDirectoryNavReq
{
    string GetServerDirectoryDEMO()
    {
        string completeServerDir = "";
        string projectPath = Directory.GetCurrentDirectory(); 
        string filePath = Path.Combine(projectPath, "root"); // root would be users/userID or however theyre organized in server dir 

        SearchDirectory(filePath, ref completeServerDir);
        return completeServerDir;
    }

    void SearchDirectory(string directory, ref string completeServerDirectory)
    {
        string[] subDirectory = Directory.GetDirectories(directory);
    
        if (subDirectory.Length == 0)
        {
            string[] currentDirFiles = Directory.GetFiles(directory);
            completeServerDirectory += directory.Split("/net9.0/")[1] += ",";
            foreach (string file in currentDirFiles)
            {
                string[] pathComponents = file.Split(Path.DirectorySeparatorChar);
                if (pathComponents[pathComponents.Length - 1] != ".DS_Store")
                    completeServerDirectory += pathComponents[pathComponents.Length - 1] += ",";
            }
        
            completeServerDirectory += '\n';
            
            return;
        }

        foreach (string dir in subDirectory)
            SearchDirectory(Path.Combine(directory, dir), ref completeServerDirectory);
    
    }
}