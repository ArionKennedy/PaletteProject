const { spawn } = require('node:child_process');

const backend = spawn(
  'dotnet',
  ['run', '--project', 'backend/Palette.Api.csproj'],
  { stdio: 'inherit', env: process.env },
);

backend.on('error', (error) => {
  console.error('Failed to start the .NET backend:', error);
  process.exitCode = 1;
});

backend.on('exit', (code, signal) => {
  process.exitCode = code ?? (signal ? 1 : 0);
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => backend.kill(signal));
}
