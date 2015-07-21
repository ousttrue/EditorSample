using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Indentation;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;

namespace EditorSample.ViewModels
{
    public class MainWindowViewModel
    {
        #region AvalonEdit
        ReactiveProperty<TextDocument> m_document;
        public ReactiveProperty<TextDocument> Document
        {
            get
            {
                if(m_document== null)
                {
                    m_document = new ReactiveProperty<TextDocument>(new TextDocument());
                }
                return m_document;
            }
        }

        public IEnumerable<IHighlightingDefinition> HighlightDefs
        {
            get
            {
                return HighlightingManager.Instance.HighlightingDefinitions;
            }
        }

        ReactiveProperty<IHighlightingDefinition> m_highlightdef;
        public ReactiveProperty<IHighlightingDefinition> HighlightDef
        {
          get {
                if(m_highlightdef== null)
                {
                    m_highlightdef = new ReactiveProperty<IHighlightingDefinition>(HighlightDefs.FirstOrDefault());
                }
                return m_highlightdef;
          }
        }

        TextEditorOptions m_options = new TextEditorOptions
        {
            
        };

        ReactiveProperty<IIndentationStrategy> m_indentationStrategy;
        public ReactiveProperty<IIndentationStrategy> IndentationStrategy
        {
            get
            {
                if(m_indentationStrategy== null)
                {
                    m_indentationStrategy = HighlightDef
                        .Select(syntaxHighlighting =>
                        {
                            switch (syntaxHighlighting.Name)
                            {
                                case "XML":
                                    return new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();

                                case "C#":
                                case "C++":
                                case "PHP":
                                case "Java":
                                    return new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(m_options);
                            }
                            return new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                        })
                        .ToReactiveProperty<IIndentationStrategy>()
                        ;
                }
                return m_indentationStrategy;
            }
        }

        ReactiveProperty<Boolean> m_isDirty;
        public ReactiveProperty<Boolean> IsDirty
        {
            get
            {
                if(m_isDirty== null)
                {
                    m_isDirty = new ReactiveProperty<bool>();
                }
                return m_isDirty;
            }
        }

        public IEnumerable<FontFamily> FontFamilies
        {
            get {
                return Fonts.SystemFontFamilies.OrderBy(x=>x.ToString());
            }
        }

        ReactiveProperty<FontFamily> m_fontFamily;
        public ReactiveProperty<FontFamily> FontFamily
        {
            get
            {
                if(m_fontFamily== null)
                {
                    m_fontFamily = new ReactiveProperty<System.Windows.Media.FontFamily>(FontFamilies.FirstOrDefault());
                }
                return m_fontFamily;
            }
        }

        public IEnumerable<Double> FontSizeList
        {
            get
            {
                return Enumerable.Range(9, 25).Select(x => (double)x);
            }
        }

        ReactiveProperty<Double> m_fontSize;
        public ReactiveProperty<Double> FontSize
        {
            get
            {
                if(m_fontSize== null)
                {
                    m_fontSize = new ReactiveProperty<double>(14);
                }
                return m_fontSize;
            }
        }
        #endregion

        ReactiveProperty<Boolean> m_isReadOnly;
        public ReactiveProperty<Boolean> IsReadOnly
        {
            get
            {
                if(m_isReadOnly == null)
                {
                    m_isReadOnly = new ReactiveProperty<bool>();
                }
                return m_isReadOnly;
            }
        }

        String m_path;
        public String Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;

                if (!File.Exists(value))
                {
                    Document.Value = new TextDocument();
                    return;
                }

                // Check file attributes and set to read-only if file attributes indicate that
                if ((System.IO.File.GetAttributes(value) & FileAttributes.ReadOnly) != 0)
                {
                    IsReadOnly.Value = true;
                }

                Document.Value = new TextDocument(File.ReadAllText(value, Encoding.UTF8).Select(x => x));
            }
        }

        ReactiveProperty<Boolean> m_hasDocument;
        public ReactiveProperty<Boolean> HasDocument
        {
            get
            {
                if(m_hasDocument== null)
                {
                    m_hasDocument = Document.Select(x => x != null).ToReactiveProperty();
                }
                return m_hasDocument;
            }
        }

        #region NewCommand
        Livet.Commands.ViewModelCommand m_newDocumentCommand;
        public ICommand NewDocumentCommand
        {
            get {
                if(m_newDocumentCommand== null)
                {
                    m_newDocumentCommand = new Livet.Commands.ViewModelCommand(NewDocument);
                }
                return m_newDocumentCommand;
            }
        }

        void NewDocument()
        {
            Path = null;
            Document.Value = new TextDocument();
        }
        #endregion

        #region OpenCommand
        Livet.Commands.ViewModelCommand m_openDocumentCommand;
        public ICommand OpenDocumentCommand
        {
            get
            {
                if (m_openDocumentCommand== null)
                {
                    m_openDocumentCommand = new Livet.Commands.ViewModelCommand(OpenDocument);
                }
                return m_openDocumentCommand;
            }
        }

        void OpenDocument()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.FileName = "Document"; // Default file name
            //dlg.DefaultExt = ".*"; // Default file extension
            dlg.Filter = String.Join("|"
                , new[]
                {
                    "Add Files(.*)|*.*",
                    "Text documents (.txt)|*.txt",
                });

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                Path = dlg.FileName;
            }
        }
        #endregion

        #region SaveCommand
        ReactiveCommand _saveDocumentCommand;
        public ICommand SaveDocumentCommand
        {
            get
            {
                if (_saveDocumentCommand == null)
                {
                    _saveDocumentCommand = HasDocument.CombineLatest(IsDirty, (x, y)=>x && y)
                        .ToReactiveCommand();
                    _saveDocumentCommand.Subscribe(OnSave);
                }
                return _saveDocumentCommand;
            }
        }

        private void OnSave(object parameter)
        {
        }
        #endregion

        #region SaveAsCommand
        ReactiveCommand _saveDocumentAsCommand;
        public ICommand SaveDocumentAsCommand
        {
            get
            {
                if (_saveDocumentAsCommand == null)
                {
                    _saveDocumentAsCommand = HasDocument.CombineLatest(IsDirty, (x, y)=>x && y)
                        .ToReactiveCommand();
                    _saveDocumentAsCommand.Subscribe(OnSaveAs);
                }
                return _saveDocumentAsCommand;
            }
        }

        private void OnSaveAs(object parameter)
        {
        }
        #endregion

        public MainWindowViewModel()
        {
            Document.Subscribe(Console.WriteLine);
        }
    }
}
