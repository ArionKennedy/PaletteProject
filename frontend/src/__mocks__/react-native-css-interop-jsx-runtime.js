const { jsx, jsxs, Fragment } = require('react/jsx-runtime');
const { createElement } = require('react');

module.exports = {
  jsx,
  jsxs,
  Fragment,
  createInteropElement: (type, props, ...children) => createElement(type, props, ...children),
};
