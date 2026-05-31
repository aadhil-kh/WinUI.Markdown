(function () {
    const state = window.__markdownSampleExtensionState || {};
    window.__markdownSampleExtensionState = state;

    function markdownSuggestions(monaco, range) {
        return [
            {
                label: "Heading 1",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "# ${1:Heading}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert an H1 heading",
                range,
                sortText: "a"
            },
            {
                label: "Heading 2",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "## ${1:Heading}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert an H2 heading",
                range,
                sortText: "b"
            },
            {
                label: "Link",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "[${1:link text}](${2:https://example.com})",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert a markdown link",
                range,
                sortText: "c"
            },
            {
                label: "Image",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "![${1:alt text}](${2:https://example.com/image.png})",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert a markdown image",
                range,
                sortText: "d"
            },
            {
                label: "Fenced code block",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "```$1\n$2\n```",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert a fenced code block",
                range,
                sortText: "e"
            },
            {
                label: "Table",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "| ${1:Column 1} | ${2:Column 2} |\n| --- | --- |\n| ${3:Value 1} | ${4:Value 2} |",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert a basic markdown table",
                range,
                sortText: "f"
            },
            {
                label: "Task list",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "- [ ] ${1:Task one}\n- [x] ${2:Task two}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert a markdown task list",
                range,
                sortText: "g"
            },
            {
                label: "Blockquote",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "> ${1:Quote}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                documentation: "Insert a blockquote",
                range,
                sortText: "h"
            }
        ];
    }

    window.configureMarkdownViewMonaco = function (context) {
        if (!context || context.language !== "markdown") {
            return;
        }

        const monaco = context.monaco;

        monaco.languages.setLanguageConfiguration("markdown", {
            brackets: [["[", "]"], ["(", ")"]],
            autoClosingPairs: [
                { open: "**", close: "**" },
                { open: "`", close: "`" },
                { open: "[", close: "]" },
                { open: "(", close: ")" },
                { open: "_", close: "_" }
            ],
            surroundingPairs: [
                { open: "**", close: "**" },
                { open: "`", close: "`" },
                { open: "[", close: "]" },
                { open: "(", close: ")" },
                { open: "_", close: "_" }
            ]
        });

        if (state.completionProvider) {
            state.completionProvider.dispose();
        }

        state.completionProvider = monaco.languages.registerCompletionItemProvider("markdown", {
            triggerCharacters: ["#", "[", "`", "|", "-", ">"],
            provideCompletionItems: function (model, position) {
                const word = model.getWordUntilPosition(position);
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };

                return {
                    suggestions: markdownSuggestions(monaco, range)
                };
            }
        });
    };
})();
