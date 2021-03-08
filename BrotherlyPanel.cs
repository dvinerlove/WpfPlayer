using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfPlayer
{
    public class BrotherlyPanel : Panel
    {
        protected override Size ArrangeOverride(Size finalSize)
        {
            var itemsHeight = finalSize.Height / Children.Count;
            for (var i = 0; i < Children.Count; i++)
                Children[i].Arrange(new Rect(0, i * itemsHeight, finalSize.Width, itemsHeight));
            Size size = new Size(0, 30);
            return finalSize;
        }
    }
}
