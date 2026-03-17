import "./style.css";
import { PanicDemoApp } from "./game";

document.querySelector<HTMLDivElement>("#app")!.innerHTML = `
  <div class="shell">
    <aside class="hud-panel">
      <div class="brand">
        <p class="eyebrow">Handle With Panic</p>
        <h1>废墟快递</h1>
        <p class="summary">2 人网页合作原型：先把跑腿小哥投过裂隙，再把货物发射过去并用毯子接住。</p>
      </div>

      <div class="card">
        <p class="card-label">Connection</p>
        <p id="connection-status">Connecting to local room server...</p>
        <button id="connect-button" type="button">Reconnect</button>
      </div>

      <div class="card">
        <p class="card-label">Role</p>
        <div class="button-row">
          <button id="role-heavy" type="button" class="role-button">Heavy Lifter</button>
          <button id="role-runner" type="button" class="role-button">Runner</button>
        </div>
        <button id="ready-button" type="button" class="primary-button">Ready Up</button>
        <button id="restart-button" type="button" class="secondary-button">Return To Lobby</button>
      </div>

      <div class="card">
        <p class="card-label">Run State</p>
        <p id="phase-label">Lobby</p>
        <p id="announcement">Waiting for room state...</p>
      </div>

      <div class="card bars">
        <div>
          <div class="bar-header">
            <span>Fragility</span>
            <span id="fragility-value">100%</span>
          </div>
          <div class="bar-track"><div id="fragility-bar" class="bar-fill fragility"></div></div>
        </div>
        <div>
          <div class="bar-header">
            <span>Stability</span>
            <span id="stability-value">100%</span>
          </div>
          <div class="bar-track"><div id="stability-bar" class="bar-fill stability"></div></div>
        </div>
      </div>

      <div class="card">
        <p class="card-label">Controls</p>
        <ul class="controls">
          <li><code>WASD</code> move</li>
          <li><code>Mouse</code> look</li>
          <li><code>F</code> grab / release cargo</li>
          <li><code>Q</code> hold blanket shield</li>
          <li><code>E</code> use catapult</li>
          <li><code>R</code> toggle fold cart</li>
          <li><code>Click</code> lock pointer</li>
        </ul>
      </div>
    </aside>

    <main class="viewport-shell">
      <div id="viewport"></div>
      <div class="crosshair"></div>
      <div class="top-ribbon">
        <span id="objective-text">Escort the cargo to the delivery ring.</span>
        <span id="player-count">Couriers: 0 / 2</span>
      </div>
    </main>
  </div>
`;

new PanicDemoApp({
  viewport: document.querySelector<HTMLDivElement>("#viewport")!,
  connectButton: document.querySelector<HTMLButtonElement>("#connect-button")!,
  roleHeavyButton: document.querySelector<HTMLButtonElement>("#role-heavy")!,
  roleRunnerButton: document.querySelector<HTMLButtonElement>("#role-runner")!,
  readyButton: document.querySelector<HTMLButtonElement>("#ready-button")!,
  restartButton: document.querySelector<HTMLButtonElement>("#restart-button")!,
  connectionStatus: document.querySelector<HTMLParagraphElement>("#connection-status")!,
  phaseLabel: document.querySelector<HTMLParagraphElement>("#phase-label")!,
  announcement: document.querySelector<HTMLParagraphElement>("#announcement")!,
  fragilityBar: document.querySelector<HTMLDivElement>("#fragility-bar")!,
  fragilityValue: document.querySelector<HTMLSpanElement>("#fragility-value")!,
  stabilityBar: document.querySelector<HTMLDivElement>("#stability-bar")!,
  stabilityValue: document.querySelector<HTMLSpanElement>("#stability-value")!,
  objectiveText: document.querySelector<HTMLSpanElement>("#objective-text")!,
  playerCount: document.querySelector<HTMLSpanElement>("#player-count")!,
});
