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

        private RowMark _mark;
        public RowMark Mark
        {
            get { return _mark; }
            set
            {
                _mark = value;
                Raise("Mark");
                Raise("IsMarked");
                Raise("IsInheritedMark");
            }
        }

        public bool IsMarked { get { return _mark == RowMark.Marked; } }
        public bool IsInheritedMark { get { return _mark == RowMark.Inherited; } }

        // 이전에 계산해 둔 값을 보여 주는 중이며 아직 새로 확인하지 않았다.
        private bool _isCached;
        public bool IsCached
        {
            get { return _isCached; }
            set { _isCached = value; Raise("IsCached"); }
        }

        private double _barFraction;
        public double BarFraction
        {
            get { return _barFraction; }
            set { _barFraction = value; Raise("BarFraction"); }
        }
    }
}
