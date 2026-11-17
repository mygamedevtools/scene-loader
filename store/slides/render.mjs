// Builds the Asset Store listing with full-bleed images: each screenshot or scene fills the canvas
// and the brand sits on top of it, instead of the design system's framed screenshot template.
//
//   node store/slides/render.mjs [path to the design-system repo]
//
// 1. Renders the pages in this folder to store/media/ (cover 1950×1300, screenshots 2400×1350).
// 2. Runs the design system's store tool on store/, for the icon, card, social image,
//    description, Third-Party Notices and checklist.
// 3. Replaces the tool's framed cover and screenshots in store/dist/ with the images from step 1.
//
// The design-system path defaults to $DESIGN_SYSTEM, then to a design-system folder next to this
// repo. Its build/ must exist (`npm run build` there), since the pages use its tokens, fonts and logo.
import { execFile } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { promisify } from 'node:util';

const here = dirname(fileURLToPath(import.meta.url));
const store = join(here, '..');
const designSystem = resolve(process.argv[2] ?? process.env.DESIGN_SYSTEM ?? join(here, '../../../design-system'));
if (!existsSync(join(designSystem, 'build/tokens.css'))) {
  console.error(`No build/tokens.css in ${designSystem}. Pass the design-system path, and run npm run build there.`);
  process.exit(1);
}

// page → [media file, dist file, CSS width, CSS height]; rendered at 2x.
const pages = {
  cover: ['cover.png', 'cover.png', 975, 650],
  '01-one-line': ['01-one-line.png', 'screenshot-01.png', 1200, 675],
  '02-mid-flight': ['02-mid-flight.png', 'screenshot-02.png', 1200, 675],
  '03-every-state': ['03-every-state.png', 'screenshot-03.png', 1200, 675],
  '04-either-source': ['04-either-source.png', 'screenshot-04.png', 1200, 675],
  '05-sample': ['05-sample.png', 'screenshot-05.png', 1200, 675],
};

const chrome = process.env.CHROME ?? [
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  '/Applications/Chromium.app/Contents/MacOS/Chromium',
  '/usr/bin/google-chrome',
  '/usr/bin/chromium',
].find((path) => existsSync(path));
if (!chrome) throw new Error('Chrome not found. Set CHROME to a Chrome or Chromium binary.');

const run = promisify(execFile);
const media = join(store, 'media');
mkdirSync(media, { recursive: true });

// Async on purpose: Chrome loads the pages from the server in this same process.
const { startServer } = await import(pathToFileURL(join(designSystem, 'scripts/serve.mjs')).href);
const server = await startServer(0, { '/slides/': here });
const base = `http://127.0.0.1:${server.address().port}/slides`;
for (const [page, [file, , width, height]] of Object.entries(pages)) {
  await run(chrome, [
    '--headless=new',
    '--disable-gpu',
    '--hide-scrollbars',
    '--force-device-scale-factor=2',
    '--virtual-time-budget=4000',
    `--window-size=${width},${height}`,
    `--screenshot=${join(media, file)}`,
    `${base}/${page}.html`,
  ], { timeout: 60000 });
  console.log(`rendered: ${relative(process.cwd(), join(media, file))}`);
}
server.close();

const { stdout } = await run('node', [join(designSystem, 'scripts/store.mjs'), store], { cwd: designSystem });
process.stdout.write(stdout);

const dist = join(store, 'dist');
for (const [file, distFile] of Object.values(pages)) copyFileSync(join(media, file), join(dist, distFile));
console.log(`replaced the framed cover and screenshots in ${relative(process.cwd(), dist)} with store/media/`);
