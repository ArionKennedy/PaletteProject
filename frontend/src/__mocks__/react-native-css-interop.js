const React = require('react');

module.exports = {
  cssInterop: (component) => component,
  remapProps: (component, _config) => component,
  createInteropElement: (type, props, ...children) =>
    React.createElement(type, props, ...children),
};
