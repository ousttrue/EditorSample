using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interactivity;

namespace EditorSample.ViewModels
{
    public class FoldingBehavior : Behavior<TextEditor>
    {
        FoldingManager m_foldingManager;
        object m_foldingStrategy;

        #region Highlighting
        public IHighlightingDefinition HighlightingDefinition
        {
            get { return (IHighlightingDefinition)GetValue(HighlightingDefinitionProperty); }
            set { SetValue(HighlightingDefinitionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightingDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightingDefinitionProperty =
            DependencyProperty.Register("HighlightingDefinition", typeof(IHighlightingDefinition), typeof(FoldingBehavior)
                , new PropertyMetadata(null, new PropertyChangedCallback(HighlightingDefinitionChanged)));

        static void HighlightingDefinitionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            (o as FoldingBehavior).HighlightingDefinitionChanged(e.NewValue as IHighlightingDefinition);
        }

        void HighlightingDefinitionChanged(IHighlightingDefinition syntaxHighlighting)
        {
            if (syntaxHighlighting == null)
            {
                m_foldingStrategy = null;
            }
            else
            {
                switch (syntaxHighlighting.Name)
                {
                    case "XML":
                        m_foldingStrategy = new XmlFoldingStrategy();
                        break;

                    case "C#":
                    case "C++":
                    case "PHP":
                    case "Java":
                        m_foldingStrategy = new BraceFoldingStrategy();
                        break;

                    default:
                        m_foldingStrategy = null;
                        break;
                }
            }

            if (m_foldingStrategy != null)
            {
                if (m_foldingManager == null)
                {
                    m_foldingManager = FoldingManager.Install(AssociatedObject.TextArea);
                }
                UpdateFoldings();
            }
            else
            {
                if (m_foldingManager != null)
                {
                    FoldingManager.Uninstall(m_foldingManager);
                    m_foldingManager = null;
                }
            }
        }

        void UpdateFoldings()
        {
            try {
                if (m_foldingStrategy is BraceFoldingStrategy)
                {
                    ((BraceFoldingStrategy)m_foldingStrategy).UpdateFoldings(m_foldingManager, AssociatedObject.Document);
                }
                if (m_foldingStrategy is XmlFoldingStrategy)
                {
                    ((XmlFoldingStrategy)m_foldingStrategy).UpdateFoldings(m_foldingManager, AssociatedObject.Document);
                }
            }
            catch(ArgumentException ex)
            {
                Console.WriteLine(ex);
            }
        }
        #endregion

        IDisposable m_timer;

        /// <summary> 
        /// ボタンにアタッチされたときに呼ばれる。 
        /// </summary> 
        protected override void OnAttached()
        {
            base.OnAttached();

            // setup
            m_timer=Observable.Interval(TimeSpan.FromSeconds(2))
                .ObserveOnDispatcher()
                .Subscribe(x => {
                    UpdateFoldings();
                })
                ;
        }

        /// <summary> 
        /// デタッチされたときに呼ばれる。 
        /// </summary> 
        protected override void OnDetaching()
        {
            // cleanup
            m_timer.Dispose();

            base.OnDetaching();
        }
    }
}
