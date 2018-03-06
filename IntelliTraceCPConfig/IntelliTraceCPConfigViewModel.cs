namespace IntelliTraceCPConfig
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml;

    public class IntelliTraceCPConfigViewModel : INotifyPropertyChanged
    {
        #region Fields

        private static XmlDocument _document;
        private static string _fileName;
        private static XmlNamespaceManager _nsMgr;

        bool? _isChecked = false;
        private bool _isExpanded;
        IntelliTraceCPConfigViewModel _parent;
        private string _tagKey;

        #endregion Fields

        #region Constructors

        IntelliTraceCPConfigViewModel(string name)
        {
            Name = name;
            Children = new List<IntelliTraceCPConfigViewModel>();
        }

        #endregion Constructors

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Properties

        public static string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
            }
        }

        public static XmlNamespaceManager NsMgr
        {
            get
            {
                if (_nsMgr == null)
                {
                    var doc = new XmlDocument();
                    _nsMgr = new XmlNamespaceManager(doc.NameTable);
                    _nsMgr.AddNamespace("msb", "urn:schemas-microsoft-com:visualstudio:tracelog");
                }
                return _nsMgr;
            }
        }

        public List<IntelliTraceCPConfigViewModel> Children
        {
            get; private set;
        }

        public bool? IsChecked
        {
            get { return _isChecked; }
            set { SetIsChecked(value, true, true); }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                _isExpanded = value;
            }
        }

        public bool IsInitiallySelected
        {
            get; private set;
        }

        public string Name
        {
            get; private set;
        }

        public string TagKey
        {
            get { return _tagKey; }
            set
            {
                _tagKey = value;
            }
        }

        #endregion Properties

        #region Methods

        public static List<IntelliTraceCPConfigViewModel> CreateTreeData()
        {
            var root = new IntelliTraceCPConfigViewModel("Configuration")
            {
                IsExpanded = true

            };

            if (string.IsNullOrEmpty(FileName))
            {
                return new List<IntelliTraceCPConfigViewModel> { root };
            }

            _document = new XmlDocument();
            _document.Load(FileName);

            XmlNodeList categorynodes = _document.GetElementsByTagName("Category");

            bool allchecked;
            foreach (XmlElement node in categorynodes)
            {
                allchecked = true;
                var par = new IntelliTraceCPConfigViewModel(node.InnerText) { _parent = root, IsInitiallySelected = false, IsExpanded = false };
                XmlNodeList childs = _document.SelectNodes("//msb:DiagnosticEventSpecification[msb:CategoryId='" + node.Attributes["Id"].Value + "']", NsMgr);
                if (childs != null)
                    foreach (XmlElement n in childs)
                    {
                        bool check;
                        if (n.Attributes["enabled"] == null)
                            check = true;
                        else
                            check = n.Attributes["enabled"].Value == "true";

                        XmlNode diag = n.SelectSingleNode("msb:SettingsName", NsMgr);
                        if (diag != null)
                            par.Children.Add(new IntelliTraceCPConfigViewModel(diag.InnerText) { TagKey = diag.Attributes["_locID"].Value, _parent = par, IsInitiallySelected = check, IsChecked = check, IsExpanded = false });
                        allchecked = allchecked && check;
                    }
                if (allchecked)
                    par.IsChecked = true;
                root.Children.Add(par);
            }
            root.Initialize();

            return new List<IntelliTraceCPConfigViewModel> { root };
        }

        public static int GetFileSizeValue()
        {
            XmlNodeList categorynodes = _document.GetElementsByTagName("MaximumLogFileSize");
            return Convert.ToInt32(categorynodes[0].InnerText);
        }

        public static XmlDocument GetModifiedDocument(List<string> modules)
        {
            PrepareModules(modules);
            return _document;
        }

        public static List<string> GetModuleList()
        {
            XmlNodeList modules = _document.SelectNodes("//msb:TraceInstrumentation/msb:ModuleList/msb:Name", NsMgr);
            return (from XmlNode module in modules select module.InnerText).ToList();
        }

        public static void SetFileSizeValue(string FileSize)
        {
            XmlNodeList categorynodes = _document.GetElementsByTagName("MaximumLogFileSize");
            categorynodes[0].InnerText = FileSize;
        }

        public List<IntelliTraceCPConfigViewModel> ShowFileContent()
        {
            OnPropertyChanged("IntelliTraceCPConfigViewModel");
            return CreateTreeData();
        }

        internal static bool AreModuleExcluded()
        {
            XmlNode node = _document.SelectSingleNode("//msb:TraceInstrumentation/msb:ModuleList", NsMgr);
            bool check;
            if (node.Attributes["isExclusionList"] == null)
                check = true;
            else
                check = node.Attributes["isExclusionList"].Value == "true";
            return check;
        }


        internal static bool GetTraceInstrumentation()
        {
            XmlNode node = _document.SelectSingleNode("//msb:TraceInstrumentation", NsMgr);
            bool check;
            if (node.Attributes["enabled"] == null)
                check = true;
            else
                check = node.Attributes["enabled"].Value == "true";
            return check;
        }

        internal static void SetTraceInstrumentation(bool enabled)
        {
            XmlNode node = _document.SelectSingleNode("//msb:TraceInstrumentation", NsMgr);

            if (node != null) node.Attributes.RemoveAll();
            XmlAttribute att = _document.CreateAttribute("enabled");
            att.Value = enabled != null && (bool)enabled ? "true" : "false";
            if (node != null) node.Attributes.Append(att);
        }


        internal static void SetModuledExcluded(bool IsExclusionList)
        {
            XmlNode node = _document.SelectSingleNode("//msb:TraceInstrumentation/msb:ModuleList", NsMgr);

            if (node != null) node.Attributes.RemoveAll();
            XmlAttribute att = _document.CreateAttribute("isExclusionList");
            att.Value = IsExclusionList != null && (bool)IsExclusionList ? "true" : "false";
            if (node != null) node.Attributes.Append(att);
        }

        private static void PrepareModules(List<string> modules)
        {
            XmlNode modulesRoot = _document.SelectSingleNode("//msb:TraceInstrumentation/msb:ModuleList", NsMgr);
            XmlNode sample = modulesRoot.ChildNodes[0];
            modulesRoot.RemoveAll();

            foreach (var module in modules)
            {
                XmlNode n = sample.Clone();
                n.InnerText = module;
                modulesRoot.AppendChild(n);
            }
        }

        void Initialize()
        {
            foreach (IntelliTraceCPConfigViewModel child in Children)
            {
                child._parent = this;
                child.Initialize();
            }
        }

        void OnPropertyChanged(string prop)
        {
            if (TagKey != null)
            {
                XmlNode oldCd = _document.SelectSingleNode("//msb:DiagnosticEventSpecification[msb:SettingsName/@_locID='" + TagKey + "']", NsMgr);

                if (oldCd != null) oldCd.Attributes.RemoveAll();
                XmlAttribute att = _document.CreateAttribute("enabled");
                var isChecked = IsChecked;
                att.Value = isChecked != null && (bool)isChecked ? "true" : "false";

                if (oldCd != null) oldCd.Attributes.Append(att);
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
                Children.ForEach(c => c.SetIsChecked(_isChecked, true, false));

            if (updateParent && _parent != null)
                _parent.VerifyCheckState();

            OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < Children.Count; ++i)
            {
                bool? current = Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            SetIsChecked(state, false, true);
        }

        #endregion Methods
    }
}