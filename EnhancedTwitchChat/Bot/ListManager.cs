using EnhancedTwitchChat.Chat;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EnhancedTwitchChat.Bot
{
    public partial class RequestBot : MonoBehaviour
    {

        #region List Manager Related functions ...

        // List types:

        // This is a work in progress. 

        // .deck = lists of songs
        // .mapper = mapper lists
        // .user = twitch user lists
        // .command = command lists = linear scripting
        // .dict = list contains key value pairs


#if UNRELEASED

    private void writelist(TwitchUser requestor, string request)
    {

    }

    // Add list to queue, filtered by InQueue and duplicatelist
    private void queuelist(TwitchUser requestor, string request)
    {
            try
            {
                StringListManager list = listcollection.OpenList(request);
                foreach (var entry in list.list) ProcessSongRequest(requestor, entry);

            }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.              

        }

        // Remove entire list from queue
        private void unqueuelist(TwitchUser requestor, string request)
    {

    }


    private void Addtolist(TwitchUser requestor, string request)
    {
        string[] parts = request.Split(new char[] { ' ', ',' }, 2);
        if (parts.Length < 2)
        {
            QueueChatMessage("Usage text... use the official help method");
            return;
        }

        try
        {

            listcollection.add(ref parts[0], ref parts[1]);
            QueueChatMessage($"Added {parts[1]} to {parts[0]}");

        }
        catch
        {
            QueueChatMessage($"list {parts[0]} not found.");
        }
    }


    private void RemoveFromlist(TwitchUser requestor, string request)
    {
        string[] parts = request.Split(new char[] { ' ', ',' }, 2);
        if (parts.Length < 2)
        {
            //     NewCommands[Addtolist].ShortHelp();
            QueueChatMessage("Usage text... use the official help method");
            return;
        }

        try
        {

            listcollection.remove(ref parts[0], ref parts[1]);
            QueueChatMessage($"Removed {parts[1]} from {parts[0]}");

        }
        catch
        {
            QueueChatMessage($"list {parts[0]} not found.");
        }
    }



    private void ClearList(TwitchUser requestor, string request)
    {

        try
        {
            listcollection.ClearList(ref request);
            QueueChatMessage($"{request} is cleared.");
        }
        catch
        {
            QueueChatMessage($"Unable to clear {request}");
        }
    }

    private void UnloadList(TwitchUser requestor, string request)
    {

        try
        {
            listcollection.ListCollection.Remove(request.ToLower());
            QueueChatMessage($"{request} unloaded.");
        }
        catch
        {
            QueueChatMessage($"Unable to unload {request}");
        }
    }


    private void ListList(TwitchUser requestor, string request)
    {

        try
        {
            var list = listcollection.OpenList(request);

            var msg = new QueueLongMessage();
            foreach (var entry in list.list) msg.Add(entry, ", ");
            msg.end("...", $"{request} is empty");
        }
        catch
        {
            QueueChatMessage($"{request} not found.");
        }
    }

    private void showlists(TwitchUser requestor, string request)
    {

        var msg = new QueueLongMessage();
        msg.Header("Loaded lists: ");
        foreach (var entry in listcollection.ListCollection) msg.Add($"{entry.Key} ({entry.Value.Count()})", ", ");
        msg.end("...", "No lists loaded.");
    }

#endif

    private void OpenList(TwitchUser requestor, string request)
        {
        listcollection.OpenList(request.ToLower());
        }


        [Flags] public enum ListFlags { ReadOnly = 1, InMemory = 2,Uncached = 4, Dynamic = 8, LineSeparator = 16,Unchanged=256};

    // The list collection maintains a dictionary of named, PERSISTENT lists. Accessing a collection by name automatically loads or crates it.
    public class ListCollectionManager
    {

        // BUG: DoNotCreate flags currently do nothing

        public Dictionary<string, StringListManager> ListCollection = new Dictionary<string, StringListManager>();

            public ListCollectionManager()
        {
            // Add an empty list so we can set various lists to empty
            StringListManager empty = new StringListManager();
            ListCollection.Add("empty", empty);
        }


        public StringListManager ClearOldList(string request, TimeSpan delta,ListFlags flags = ListFlags.Unchanged)
            {
            string listfilename = Path.Combine(datapath, request);
            TimeSpan UpdatedAge = GetFileAgeDifference(listfilename);
          
            StringListManager list = OpenList(request, flags);

            if (UpdatedAge>delta)
                    {
                    RequestBot.Instance.QueueChatMessage($"Clearing old session {request}");
                    list.Clear();
                    }

            return list;
            }

        public StringListManager OpenList(string request, ListFlags flags = ListFlags.Unchanged) // All lists are accessed through here, flags determine mode
        {
            StringListManager list;
            if (!ListCollection.TryGetValue(request, out list))
            {
                list = new StringListManager();
                ListCollection.Add(request, list);
                if (!flags.HasFlag(ListFlags.InMemory)) list.Readfile(request); // If in memory, we never read from disk
            }
            else
            {
                if (flags.HasFlag(ListFlags.Uncached)) list.Readfile(request); // If Cache is off, ALWAYS re-read file.
            }
            return list;
        }

        public bool contains(ref string listname, string key, ListFlags flags = ListFlags.Unchanged)
        {
            try
            {
                StringListManager list = OpenList(listname);
                return list.Contains(key);
            }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.              

            return false;
        }

        public bool add(string listname, string key, ListFlags flags = ListFlags.Unchanged)
            {
            return add(ref listname, ref key, flags);
            }

        public bool add(ref string listname, ref string key, ListFlags flags = ListFlags.Unchanged)
        {
            try
            {
                StringListManager list = OpenList(listname);

                list.Add(key);        
        
                if (!flags.HasFlag(ListFlags.InMemory | ListFlags.ReadOnly)) list.Writefile(listname);
                return true;

            }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.              

            return false;
        }

            public bool remove(string listname, string key, ListFlags flags = ListFlags.Unchanged)
            {
                return remove(ref listname, ref key, flags);
            }
            public bool remove(ref string listname, ref string key, ListFlags flags = ListFlags.Unchanged)
        {
            try
            {
                StringListManager list = OpenList(listname);

                list.Removeentry(key);

                if (!flags.HasFlag(ListFlags.InMemory | ListFlags.ReadOnly)) list.Writefile(listname);
                return false;

            }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.              

            return false;
        }


        public void runscript(string listname, ListFlags flags = ListFlags.Unchanged)
        {

            try
            {
                 OpenList(listname, flags).runscript();
               
            }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.              
        }


        public void ClearList(ref string listname, ListFlags flags = ListFlags.Unchanged)
        {
            try
            {
                ListCollection[listname].Clear();
            }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.              
        }

    }

    public static ListCollectionManager listcollection = new ListCollectionManager();



    // All variables are public for now until we finalize the interface
    public class StringListManager
    {

        private static char[] anyseparator = { ',', ' ', '\t', '\r', '\n' };
        private static char[] lineseparator = { '\n', '\r' };

        public List<string> list = new List<string>();
        private HashSet<string> hashlist = new HashSet<string> ();
       

        ListFlags flags = 0;

        // Callback function prototype here

        public StringListManager(ListFlags ReadOnly = ListFlags.Unchanged)
        {

        }

        public bool Readfile(string filename, bool ConvertToLower = true)
        {
            if (flags.HasFlag(ListFlags.InMemory)) return false;

            try
            {
                string listfilename = Path.Combine(datapath, filename);
                string fileContent = File.ReadAllText(listfilename);
                if (listfilename.EndsWith(".script"))
                    list = fileContent.Split(lineseparator, StringSplitOptions.RemoveEmptyEntries).ToList();
                else
                    list = fileContent.Split(anyseparator, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (ConvertToLower) LowercaseList();
                return true;
            }
            catch
            {
                // Ignoring this for now, I expect it to fail
            }

            return false;
        }

        public void runscript()
            {

            try
                {
                foreach (var line in list) Parse(TwitchWebSocketClient.OurTwitchUser, line);
                }
            catch (Exception ex) { Plugin.Log(ex.ToString()); } // Going to try this form, to reduce code verbosity.            
  
            }


            public bool Writefile(string filename)
        {

            string separator = filename.EndsWith(".script") ? "\n" : ",";

            try
            {
                string listfilename = Path.Combine(datapath, filename);

                var output = String.Join(separator, list.ToArray());
                File.WriteAllText(listfilename, output);
                return true;
            }
            catch
            {
                // Ignoring this for now, failed write can be silent
            }

            return false;
        }


        public bool Contains(string entry)
            {
                if (list.Contains(entry)) return true;
                return false;
            }
            public bool Add(string entry)
        {
            if (list.Contains(entry)) return false;
            list.Add(entry);
            return true;
        }

        public bool Removeentry(string entry)
        {
            return list.Remove(entry);
        }

        // Picks a random entry and returns it, removing it from the list
        public string Drawentry()
        {
            if (list.Count == 0) return "";
            int entry = generator.Next(0, list.Count);
            string result = list.ElementAt(entry);
            list.RemoveAt(entry);
            return result;
        }

        // Picks a random entry but does not remove it
        public string Randomentry()
        {
            if (list.Count == 0) return "";
            int entry = generator.Next(0, list.Count);
            string result = list.ElementAt(entry);
            return result;
        }

        public int Count()
        {
            return list.Count;
        }

        public void Clear()
        {
            list.Clear();
        }

        public void LowercaseList()
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = list[i].ToLower();
            }
        }
        public void Outputlist(ref QueueLongMessage msg, string separator = ", ")
        {
            foreach (string entry in list) msg.Add(entry, separator);
        }

    }


     #endregion


    }
}