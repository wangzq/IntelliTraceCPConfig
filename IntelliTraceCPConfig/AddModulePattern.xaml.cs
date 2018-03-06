using System.Windows;

namespace IntelliTraceCPConfig
{
    /// <summary>
    /// Interaction logic for AddModulePattern.xaml
    /// </summary>
    public partial class AddModulePattern : Window
    {
        public AddModulePattern()
        {
            InitializeComponent();
        }

        private void BtnAddClick(object sender, RoutedEventArgs e)
        {
            var parent = Owner as Main;
            if (txtPattern.Text.Trim().Length > 0)
                if (parent != null) parent.AddPattern(txtPattern.Text);

            Close();
        }
    }
}
