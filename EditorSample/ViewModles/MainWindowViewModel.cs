using ICSharpCode.AvalonEdit.Document;
using Reactive.Bindings;
using System;
using System.Reactive.Linq;
using System.Windows.Input;

namespace EditorSample.ViewModles
{
    class MainWindowViewModel
    {
        ReactiveProperty<TextDocument> m_document;
        public ReactiveProperty<TextDocument> Document
        {
            get
            {
                if(m_document== null)
                {
                    m_document = new ReactiveProperty<TextDocument>();
                }
                return m_document;
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
            Document.Value = new TextDocument();
        }

        public MainWindowViewModel()
        {
            Document.Subscribe(Console.WriteLine);
        }
    }
}
