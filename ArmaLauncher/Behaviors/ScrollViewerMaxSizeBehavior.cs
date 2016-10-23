using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace ArmaLauncher.Behaviors
{
    
    /*
    Container for infinite-sized objects (such as a ScrollViewer)
    
    Here's how to use it in a XAML file (assuming the above class was defined in a namespace known as "custom" within the XML namespace):
    
    Somewhere at the top: 
        xmlns:Interactivity="clr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity"
    
    Then:
    
        <ScrollViewer Style="{StaticResource AppHost_ScrollViewer}">
            <Interactivity:Interaction.Behaviors>
                <custom:ScrollViewerMaxSizeBehavior MinContentWidth="600"
                                                    MinContentHeight="500"/>
            </Interactivity:Interaction.Behaviors>
    
            <!-- content -->
    
        </ScrollViewer>
        
    */
    
    public class ScrollViewerMaxSizeBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// Convenience method for retrieving the ScrollViewerMaxSizeBehavior object that may (or may not) be applied to the given ScrollViewer.
        /// Returns null if none is applied.
        /// </summary>
        public static ScrollViewerMaxSizeBehavior GetScrollViewerMaxSizeBehavior(ScrollViewer scrollViewer)
        {
            return Interaction.GetBehaviors(scrollViewer)
                              .Select(x => x as ScrollViewerMaxSizeBehavior)
                              .FirstOrDefault(x => null != x);
        }
 
        public static readonly DependencyProperty MinContentHeightProperty = DependencyProperty.Register("MinContentHeight", typeof(int),
            typeof(ScrollViewerMaxSizeBehavior), new UIPropertyMetadata() { PropertyChangedCallback = MinSizeChanged });
 
        public int MinContentHeight
        {
            get { return (int)GetValue(MinContentHeightProperty); }
            set { SetValue(MinContentHeightProperty, value); }
        }
 
        public static readonly DependencyProperty MinContentWidthProperty = DependencyProperty.Register("MinContentWidth", typeof(int),
            typeof(ScrollViewerMaxSizeBehavior), new UIPropertyMetadata() { PropertyChangedCallback = MinSizeChanged });
 
        public int MinContentWidth
        {
            get { return (int)GetValue(MinContentWidthProperty); }
            set { SetValue(MinContentWidthProperty, value); }
        }
 
        protected static void MinSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as ScrollViewerMaxSizeBehavior;
            if (null == self)
            {
                return;
            }
            self.Update();
        }
 
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SizeChanged += this.ParentSizeChanged;
            this.Update();
        }
 
        protected override void OnDetaching()
        {
            this.AssociatedObject.SizeChanged -= this.ParentSizeChanged;
            base.OnDetaching();
        }
 
        protected void ParentSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            this.Update();
        }
 
        private void Update()
        {
            if (null == this.AssociatedObject)
            {
                return;
            }
            var content = this.AssociatedObject.Content as FrameworkElement;
 
            if ((0 >= this.AssociatedObject.ActualHeight)
                || (0 >= this.AssociatedObject.ActualWidth))
            {
                // The attached ScrollViewer was probably not laid out yet, or has zero size.
                this.AssociatedObject.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                this.AssociatedObject.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                return;
            }
 
            int minHeight = this.MinContentHeight;
            int minWidth = this.MinContentWidth;
 
            if ((minHeight <= 0) || (minWidth <= 0))
            {
                // Probably our attached properties were not initialized. By default we disable the scrolling completely,
                // to prevent exceptions from infinitely-sized objects within us.
                this.AssociatedObject.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                this.AssociatedObject.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                return;
            }
 
            this.AssociatedObject.SizeChanged -= this.ParentSizeChanged;
 
            if (this.AssociatedObject.ActualHeight < minHeight)
            {
                this.AssociatedObject.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                if (null != content)
                {
                    content.MaxHeight = minHeight - (content.Margin.Bottom + content.Margin.Top);
                }
            }
            else
            {
                this.AssociatedObject.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                if (null != content)
                {
                    content.MaxHeight = Double.PositiveInfinity;
                }
            }
 
            if (this.AssociatedObject.ActualWidth < minWidth)
            {
                this.AssociatedObject.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                if (null != content)
                {
                    content.MaxWidth = minWidth - (content.Margin.Left + content.Margin.Right);
                }
            }
            else
            {
                this.AssociatedObject.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                if (null != content)
                {
                    content.MaxWidth = Double.PositiveInfinity;
                }
            }
 
            this.AssociatedObject.SizeChanged += this.ParentSizeChanged;
        }
    }
}
