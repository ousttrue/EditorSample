using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Reactive.Bindings;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace EditorSample.ViewModels
{
    public class FoldingBehavior : Behavior<TextEditor>
    {
        ReactiveProperty<TextDocument> m_foldingDocumnet;
        ReactiveProperty<TextDocument> FoldingDocument
        {
            get {
                if(m_foldingDocumnet== null)
                {
                    m_foldingDocumnet = new ReactiveProperty<TextDocument>();
                    m_foldingDocumnet.Subscribe(_ =>
                    {
                        Manager = null;
                    });
                }
                return m_foldingDocumnet;
            }
        }

        FoldingManager m_foldingManager;
        FoldingManager Manager
        {
            get { return m_foldingManager; }
            set {
                if (m_foldingManager == value) return;
                if (m_foldingManager != null)
                {
                    Console.WriteLine("Uninstall");
                    FoldingManager.Uninstall(m_foldingManager);
                }
                m_foldingManager = value;
            }
        }

        ReactiveProperty<Object> m_foldingStrategy;
        ReactiveProperty<Object> FoldingStrategy
        {
            get {
                if(m_foldingStrategy==null)
                {
                    m_foldingStrategy = new ReactiveProperty<object>();
                    m_foldingStrategy.Subscribe(s =>
                    {
                        Manager = null;

                        if (s == null)
                        {
                            m_currentFolding = null;
                        }
                        else
                        {
                            var t = s.GetType();
                            var mi = t.GetMethod("UpdateFoldings"
                                , BindingFlags.Public | BindingFlags.Instance
                                );
                            m_currentFolding = (m, d) =>
                            {
                                if (m == null) return;
                                if (d == null) return;
                                mi.Invoke(s, new Object[] { m, d });
                            };
                        }
                    });
                }
                return m_foldingStrategy;
            }
        }

        #region HighlightingDefinition
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

        void HighlightingDefinitionChanged(IHighlightingDefinition newValue)
        {
            FoldingStrategy.Value = GetFoldingStrategy(newValue);
        }

        static Object GetFoldingStrategy(IHighlightingDefinition syntaxHighlighting)
        {
            if (syntaxHighlighting == null)return null;

            switch (syntaxHighlighting.Name)
            {
                case "XML":
                    return new XmlFoldingStrategy();

                case "C#":
                case "C++":
                case "PHP":
                case "Java":
                    return new BraceFoldingStrategy();

                default:
                    return null;
            }
        }
        #endregion

        public delegate void UpdateFoldings(FoldingManager manager, TextDocument document);
        UpdateFoldings m_currentFolding;

        /// <summary> 
        /// ボタンにアタッチされたときに呼ばれる。 
        /// </summary> 
        protected override void OnAttached()
        {
            AssociatedObject.DocumentChanged += (o, e) =>
              {
                  OnUpdate();
              };

            // Binding
            //AssociatedObject.SyntaxHighlighting.
            var myBinding = new Binding("SyntaxHighlighting");
            myBinding.Source = AssociatedObject;
            BindingOperations.SetBinding(this, FoldingBehavior.HighlightingDefinitionProperty, myBinding);

            FoldingStrategy.Subscribe(_ =>
            {
                OnUpdate();
            });

            base.OnAttached();
        }

        void OnUpdate()
        {
            FoldingDocument.Value = AssociatedObject.Document;

            if (m_currentFolding == null) return;
            try
            {
                if (Manager == null)
                {
                    Manager = FoldingManager.Install(AssociatedObject.TextArea);
                }
                m_currentFolding(Manager, AssociatedObject.Document);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary> 
        /// デタッチされたときに呼ばれる。 
        /// </summary> 
        protected override void OnDetaching()
        {
            base.OnDetaching();
        }
    }
}
