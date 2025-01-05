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
            var length = _lastFiles.Count - 1;
            var indexFile = length - index;
            return _lastFiles[indexFile];
        }

        public void AddBackupFilename(string date, string map)
        {
            var length = _lastFiles.Count;
            _lastFiles.Add(FormatFilename(date, map));
        }
    }
}
