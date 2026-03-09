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
const destPath = upath.resolve(upath.dirname(__filename), '../wwwroot/dist/css/ShareSmallBiz.min.css');

module.exports = function renderSCSS() {
    // NOTE: @import deprecation is silenced intentionally.
    // The full migration from @import to @use/@forward in the ~50 SCSS partials
    // requires sass-migrator or a systematic manual update (each partial must
    // @use its own variable dependencies). Track in: https://sass-lang.com/d/import
    const results = sass.compileString(entryPoint, {
        loadPaths: [
            upath.resolve(upath.dirname(__filename), '../node_modules')
        ],
        // 'import'       — @import migration is deferred; requires updating ~50 partials (sass-migrator)
        // 'global-builtin', 'color-functions' — Bootstrap 5.x uses deprecated lighten()/darken()
        silenceDeprecations: ['import', 'global-builtin', 'color-functions'],
    });

    const destPathDirname = upath.dirname(destPath);
    if (!sh.test('-e', destPathDirname)) {
        sh.mkdir('-p', destPathDirname);
    }

    postcss([
        autoprefixer,
        cssnano(),
    ]).process(results.css, { from: 'styles.css', to: 'ShareSmallBiz.min.css' })
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
