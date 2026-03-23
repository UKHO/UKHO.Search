const fs = require('fs');
const path = require('path');

const sourceDirectory = path.resolve(__dirname, '..', 'src', 'browser', 'assets');
const destinationDirectory = path.resolve(__dirname, '..', 'lib', 'browser', 'assets');

if (!fs.existsSync(sourceDirectory)) {
    process.exit(0);
}

fs.rmSync(destinationDirectory, {
    recursive: true,
    force: true
});

fs.mkdirSync(path.dirname(destinationDirectory), {
    recursive: true
});

fs.cpSync(sourceDirectory, destinationDirectory, {
    recursive: true
});
