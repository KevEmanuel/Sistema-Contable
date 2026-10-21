/*using System.Windows.Controls;

namespace SistemaContableDTE.Views;

public partial class CatalogoCuentasView : UserControl
{
    public CatalogoCuentasView()
    {
        InitializeComponent();
    }
}  */

using System.Windows;
using System.Windows.Controls;

namespace SistemaContableDTE.Views;

public partial class CatalogoCuentasView : UserControl
{
    public CatalogoCuentasView()
    {
        InitializeComponent();
    }

    private void VolverAlPanelPrincipal_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is not null)
        {
            Application.Current.MainWindow.Content =
                new PanelPrincipalView();
        }
    }
}
