const path = require('path');
const puppeteer = require('puppeteer-core');

const [htmlArg, pdfPath, mode] = process.argv.slice(2);
const htmlPath = path.resolve(htmlArg);

const footer = `
<div style="width:100%;font-family:'Segoe UI',Arial,sans-serif;font-size:7.5pt;
            color:#8794a1;padding:2mm 18mm 0 18mm;margin:0 0 3mm 0;
            display:flex;justify-content:space-between;border-top:1px solid #e3e8ed;">
  <span>Service Management Platform &nbsp;&middot;&nbsp; Software Specification v1.0</span>
  <span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span>
</div>`;

const options = {
  cover: {
    printBackground: true,
    displayHeaderFooter: false,
    margin: { top: 0, right: 0, bottom: 0, left: 0 },
  },
  body: {
    printBackground: true,
    displayHeaderFooter: true,
    headerTemplate: '<div></div>',
    footerTemplate: footer,
    margin: { top: '18mm', right: '18mm', bottom: '18mm', left: '18mm' },
  },
};

(async () => {
  const browser = await puppeteer.launch({
    executablePath: process.env.CHROME_PATH || '/usr/local/bin/google-chrome',
    headless: 'new',
    args: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage'],
  });

  const page = await browser.newPage();
  await page.goto(`file://${htmlPath}`, { waitUntil: 'networkidle0' });
  await page.pdf({ path: pdfPath, format: 'A4', ...options[mode] });

  await browser.close();
  console.log(`wrote ${pdfPath} (${mode})`);
})().catch((err) => {
  console.error(err);
  process.exit(1);
});
