﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using text_fmg.Properties;
using Kontract.Interface;
using Kontract;
using Kontract.IO;

namespace text_fmg
{
    public sealed class FmgAdapter : ITextAdapter
    {
        private FileInfo _fileInfo = null;
        private FMG _fmg = null;
        private FMG _fmgBackup = null;
        private List<Entry> _entries = null;

        #region Properties

        // Information
        public string Name => "FMG";
        public string Description => "Monster Hunter Nikki Text";
        public string Extension => "*.fmg";
        public string About => "This is the FMG text adapter for Kuriimu.";

        // Feature Support
        public bool FileHasExtendedProperties => false;
        public bool CanSave => true;
        public bool CanAddEntries => false;
        public bool CanRenameEntries => false;
        public bool CanDeleteEntries => false;
        public bool CanSortEntries => true;
        public bool EntriesHaveSubEntries => false;
        public bool EntriesHaveUniqueNames => true;
        public bool EntriesHaveExtendedProperties => false;

        public FileInfo FileInfo
        {
            get
            {
                return _fileInfo;
            }
            set
            {
                _fileInfo = value;
            }
        }

        public string LineEndings => "\n";

        #endregion

        public bool Identify(string filename)
        {
            using (BinaryReaderX br = new BinaryReaderX(File.OpenRead(filename)))
            {
                var magic = br.ReadInt32();
                var fileSize = br.ReadInt32();

                return (magic == 0x00010000 && fileSize == br.BaseStream.Length);
            }
        }

        //Load file, make backup if needed
        public LoadResult Load(string filename, bool autoBackup = false)
        {
            LoadResult result = LoadResult.Success;

            _fileInfo = new FileInfo(filename);
            _entries = null;

            if (_fileInfo.Exists)
            {
                _fmg = new FMG(_fileInfo.FullName);

                string backupFilePath = _fileInfo.FullName + ".bak";
                if (File.Exists(backupFilePath))
                {
                    _fmgBackup = new FMG(backupFilePath);
                }
                else if (autoBackup || MessageBox.Show("Would you like to create a backup of " + _fileInfo.Name + "?\r\nA backup allows the Original text box to display the source text before edits were made.", "Create Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    File.Copy(_fileInfo.FullName, backupFilePath);
                    _fmgBackup = new FMG(backupFilePath);
                }
                else
                {
                    _fmgBackup = null;
                }
            }
            else
                result = LoadResult.FileNotFound;

            return result;
        }

        public SaveResult Save(string filename = "")
        {
            SaveResult result = SaveResult.Success;

            if (filename.Trim() != string.Empty)
                _fileInfo = new FileInfo(filename);

            try
            {
                _fmg.Save(_fileInfo.FullName);
            }
            catch (Exception)
            {
                result = SaveResult.Failure;
            }

            return result;
        }

        // Entries
        public IEnumerable<TextEntry> Entries
        {
            get
            {
                if (_entries == null)
                {
                    _entries = new List<Entry>();

                    foreach (Label label in _fmg.Labels)
                    {
                        if (_fmgBackup == null)
                        {
                            Entry entry = new Entry(label);
                            _entries.Add(entry);
                        }
                        else
                        {
                            Entry entry = new Entry(label, _fmgBackup.Labels.FirstOrDefault(o => o.TextID == label.TextID));
                            _entries.Add(entry);
                        }
                    }
                }

                if (SortEntries)
                    return _entries.OrderBy(e => e.Name).ThenBy(e => e.EditedLabel.TextID);

                return _entries;
            }
        }

        public IEnumerable<string> NameList => Entries?.Select(o => o.Name);

        public string NameFilter => @".*";

        public int NameMaxLength => 0;

        // Features
        public bool ShowProperties(Icon icon) => false;

        public TextEntry NewEntry() => new Entry();

        public bool AddEntry(TextEntry entry) => false;

        public bool RenameEntry(TextEntry entry, string newName) => false;

        public bool DeleteEntry(TextEntry entry) => false;

        public bool ShowEntryProperties(TextEntry entry, Icon icon) => false;

        // Settings
        public bool SortEntries
        {
            get { return Settings.Default.SortEntries; }
            set
            {
                Settings.Default.SortEntries = value;
                Settings.Default.Save();
            }
        }
    }
}
