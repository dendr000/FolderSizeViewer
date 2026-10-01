// Copyright (c) dendr000. MIT License.
using System.ComponentModel;

namespace FolderSizeViewer
{
    public class RowItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise(string name)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        public string FullPath;
        public bool IsDirectory { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; Raise("Name"); }
        }

        private string _typeLabel;
        public string TypeLabel
        {
            get { return _typeLabel; }
            set { _typeLabel = value; Raise("TypeLabel"); }
        }

        private long _sizeBytes;
        public long SizeBytes
        {
            get { return _sizeBytes; }
            set { _sizeBytes = value; Raise("SizeBytes"); }
        }

        private string _sizeText;
        public string SizeText
        {
            get { return _sizeText; }
            set { _sizeText = value; Raise("SizeText"); }
        }

        private bool _isCalculating;
        public bool IsCalculating
        {
            get { return _isCalculating; }
            set { _isCalculating = value; Raise("IsCalculating"); }
        }

        private double _barFraction;
        public double BarFraction
        {
            get { return _barFraction; }
            set { _barFraction = value; Raise("BarFraction"); }
        }
    }
}
