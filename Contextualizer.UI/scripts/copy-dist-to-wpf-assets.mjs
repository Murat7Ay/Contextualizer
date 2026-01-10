import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function exists(p) {
  try {
    await fs.access(p);
    return true;
  } catch {
    return false;
  }
}

async function main() {
  const uiRoot = path.resolve(__dirname, '..');
  const srcDist = path.join(uiRoot, 'dist');
  const srcIndex = path.join(srcDist, 'index.html');

  const repoRoot = path.resolve(uiRoot, '..');
  const wpfAppDir = path.join(repoRoot, 'WpfInteractionApp');
  const dstDist = path.join(wpfAppDir, 'Assets', 'Ui', 'dist');

  if (!(await exists(srcIndex))) {
    console.warn(`[copy-dist] Skipping: build output not found at ${srcIndex}`);
    return;
  }

  if (!(await exists(wpfAppDir))) {
    console.warn(`[copy-dist] Skipping: WpfInteractionApp not found at ${wpfAppDir}`);
    return;
  }

  await fs.rm(dstDist, { recursive: true, force: true });
  await fs.mkdir(path.dirname(dstDist), { recursive: true });
  await fs.cp(srcDist, dstDist, { recursive: true });

  console.log(`[copy-dist] Copied ${srcDist} -> ${dstDist}`);
}

await main();


