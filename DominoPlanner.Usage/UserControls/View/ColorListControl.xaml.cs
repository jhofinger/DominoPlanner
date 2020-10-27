﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DominoPlanner.Usage.UserControls.View
{
    public class ColorListControl : UserControl
    {
        public ColorListControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
