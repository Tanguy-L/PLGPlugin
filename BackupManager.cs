namespace PLGPlugin
{
    public class BackupManager
    {
        private List<string> _lastFiles = [];
        private string prefixFilename = "plg_";

        public string FormatFilename(string date, string map)
        {
            return prefixFilename + "_" + date + "_" + map + ".txt";
        }

        // Invert index
        public string GetFilenameByIndex(int index)
        {
            int length = _lastFiles.Count - 1;
            int indexFile = length - index;
            return _lastFiles[indexFile];
        }

        public void AddBackupFilename(string date, string map)
        {
            int length = _lastFiles.Count;
            _lastFiles.Add(FormatFilename(date, map));
        }
    }
}
