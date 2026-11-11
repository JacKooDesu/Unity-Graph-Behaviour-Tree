using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Text;

namespace BehaviourTreeEditor.Runtime
{
    /// <summary>
    /// 用於輸出 C# 腳本，管理縮排與提供 API
    /// </summary>
    public class ScriptBuilder : IDisposable
    {
        const int INDENT_LEVEL = 1;
        readonly StringBuilder _sb;
        readonly StringWriter _stringWriter;
        readonly IndentedTextWriter _indentWriter;

        public ScriptBuilder()
        {
            _sb = new();
            _stringWriter = new StringWriter(_sb);
            _indentWriter = new IndentedTextWriter(_stringWriter);
        }

        public void Declare(string left, string right, bool byRef = true)
        {
            if (byRef)
                _indentWriter.WriteLine($"ref var {left} = ref {right};");
            else
                _indentWriter.WriteLine($"var {left} = {right};");
        }

        public void BeginScope()
        {
            _indentWriter.WriteLine("{");
            _indentWriter.Indent += INDENT_LEVEL;
        }

        public void EndScope()
        {
            _indentWriter.Indent -= INDENT_LEVEL;
            _indentWriter.WriteLine("}");
        }

        public void AppendLine(string line)
        {
            _indentWriter.WriteLine(line);
        }

        public string GetScript()
        {
            return _stringWriter.ToString();
        }

        public void Dispose()
        {
            _indentWriter.Dispose();
            _stringWriter.Dispose();
        }
    }
}
