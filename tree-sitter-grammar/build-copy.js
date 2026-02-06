const fs = require('fs');

const isWin = process.platform === 'win32';
const ext = isWin ? 'dll' : 'so';

// Create necessary directories
['../nvim-plugin/queries/csasm', '../nvim-plugin/parser'].forEach(dir => {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
});

// Copy files
fs.copyFileSync('queries/highlights.scm', '../nvim-plugin/queries/csasm/highlights.scm');
fs.copyFileSync(`build/csasm.${ext}`, `../nvim-plugin/parser/csasm.${ext}`);
