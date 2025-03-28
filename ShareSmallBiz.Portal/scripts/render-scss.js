'use strict';
const autoprefixer = require('autoprefixer');
const fs = require('fs');
const packageJSON = require('../package.json');
const upath = require('upath');
const postcss = require('postcss');
const sass = require('sass');
const sh = require('shelljs');
const cssnano = require('cssnano');

const stylesPath = '../src/scss/styles.scss';
const destPath = upath.resolve(upath.dirname(__filename), '../wwwroot/dist/css/ShareSmallBiz.min.css'); // Updated to .min.css

module.exports = function renderSCSS() {
    
    const results = sass.renderSync({
        data: entryPoint,
        includePaths: [
            upath.resolve(upath.dirname(__filename), '../node_modules')
        ],
    });

    const destPathDirname = upath.dirname(destPath);
    if (!sh.test('-e', destPathDirname)) {
        sh.mkdir('-p', destPathDirname);
    }

    postcss([
        autoprefixer,
        cssnano(), // This minifies the CSS
    ]).process(results.css, { from: 'styles.css', to: 'ShareSmallBiz.min.css' }) // Updated to match the minified output
      .then(result => {
        result.warnings().forEach(warn => {
            console.warn(warn.toString());
        });
        fs.writeFileSync(destPath, result.css.toString());
        console.log(`CSS compiled and minified to: ${destPath}`);
    });
};

const entryPoint = `/*!
* Start Bootstrap - ${packageJSON.title} v${packageJSON.version} (${packageJSON.homepage})
* Copyright 2013-${new Date().getFullYear()} ${packageJSON.author}
* Licensed under ${packageJSON.license} (https://github.com/BlackrockDigital/${packageJSON.name}/blob/master/LICENSE)
*/
@import "${stylesPath}"
`;
