// Prism theme built on the design system's code tokens (--mgt-color-code-*).
// Code blocks are ink in both color modes, so the same theme serves light and dark; the
// variables still switch with the mode (dark mode uses the sunken ink background).

/** @type {import('prism-react-renderer').PrismTheme} */
const mgtPrismTheme = {
  plain: {
    color: 'var(--mgt-color-code-text)',
    backgroundColor: 'var(--mgt-color-code-bg)',
  },
  styles: [
    {
      types: ['comment', 'prolog', 'doctype', 'cdata'],
      style: { color: 'var(--mgt-color-code-comment)', fontStyle: 'italic' },
    },
    {
      types: ['keyword', 'class-name', 'builtin', 'namespace', 'tag', 'selector', 'important', 'atrule'],
      style: { color: 'var(--mgt-color-code-keyword)' },
    },
    {
      types: ['string', 'char', 'attr-value', 'regex', 'url', 'number', 'boolean', 'constant'],
      style: { color: 'var(--mgt-color-code-string)' },
    },
    {
      types: ['punctuation', 'operator'],
      style: { color: 'var(--mgt-color-code-comment)' },
    },
    // Diff lines. The code background is always ink, so these use the dark-mode status
    // text colors directly; they're the only status colors that read on it in both modes.
    {
      types: ['inserted'],
      style: { color: '#59A549' },
    },
    {
      types: ['deleted'],
      style: { color: '#E26674' },
    },
  ],
};

export default mgtPrismTheme;
