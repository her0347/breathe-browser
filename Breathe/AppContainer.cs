using EasyTabs;
using System;

namespace Breathe
{
    public partial class AppContainer : TitleBarTabs
    {
        public AppContainer()
        {
            InitializeComponent();

            AeroPeekEnabled = true;
            TabRenderer = new ChromeTabRenderer(this);
        }


        public override TitleBarTab CreateTab()
        {
            return new TitleBarTab(this)
            {
                // The content will be an instance of another Form
                // In our example, we will create a new instance of the Form1
                Content = new Form1
                {
                    Text = "New Tab"
                }
            };
        }

        private void AppContainer_Load(object sender, EventArgs e)
        {

        }
    }
}
