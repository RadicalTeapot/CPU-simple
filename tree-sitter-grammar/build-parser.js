const fs = require('fs');
const { execSync } = require('child_process');

const isWin = process.platform === 'win32';
const ext = isWin ? 'dll' : 'so';

if (!fs.existsSync('build')) {
  fs.mkdirSync('build', { recursive: true });
}

execSync(`tree-sitter build -o build/csasm.${ext}`, { stdio: 'inherit' });
