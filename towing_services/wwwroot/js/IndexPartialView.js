// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


/*!
 * Select2 4.1.0
 * https://select2.org
 *
 * Released under the MIT license
 * https://github.com/select2/select2/blob/develop/LICENSE.md
 */
(function (factory) {
    if (typeof define === 'function' && define.amd) {
        define(['jquery'], factory);
    } else if (typeof module === 'object' && module.exports) {
        module.exports = function (root, jQuery) {
            if (jQuery === undefined) {
                if (typeof window !== 'undefined') {
                    jQuery = require('jquery');
                } else {
                    jQuery = require('jquery')(root);
                }
            }
            factory(jQuery);
            return jQuery;
        };
    } else {
        factory(jQuery);
    }
}(function ($) {
    // Select2 core implementation
    $.fn.select2 = function (options) {
        // Example: replace this with the actual Select2 functionality
        console.log("Select2 initialized with options:", options);
    };
    // Example implementation for testing:
    $.fn.select2.defaults = {
        placeholder: "Select an option",
        allowClear: true
    };
}));


