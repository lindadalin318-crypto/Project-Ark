import * as THREE from "three";
import { Client, Room } from "@colyseus/sdk";

type RoleType = "heavy" | "runner";
type PhaseType = "lobby" | "playing" | "completed" | "failed";

interface PanicDemoAppElements {
  viewport: HTMLDivElement;
  connectButton: HTMLButtonElement;
  roleHeavyButton: HTMLButtonElement;
  roleRunnerButton: HTMLButtonElement;
  readyButton: HTMLButtonElement;
  restartButton: HTMLButtonElement;
  connectionStatus: HTMLParagraphElement;
  phaseLabel: HTMLParagraphElement;
  announcement: HTMLParagraphElement;
  fragilityBar: HTMLDivElement;
  fragilityValue: HTMLSpanElement;
  stabilityBar: HTMLDivElement;
  stabilityValue: HTMLSpanElement;
  objectiveText: HTMLSpanElement;
  playerCount: HTMLSpanElement;
}

interface PlayerSnapshot {
  sessionId: string;
  name: string;
  role: RoleType;
  ready: boolean;
  connected: boolean;
  blanketActive: boolean;
  yaw: number;
  catapultCooldown: number;
  position: { x: number; y: number; z: number };
}

interface CargoSnapshot {
  position: { x: number; y: number; z: number };
  velocity: { x: number; y: number; z: number };
  fragility: number;
  stability: number;
  isCarried: boolean;
  carrierId: string;
  cartLatched: boolean;
  panic: boolean;
  airborne: boolean;
}

interface RoomSnapshot {
  players: Record<string, PlayerSnapshot>;
  cargo: CargoSnapshot;
  phase: PhaseType;
  elapsedTime: number;
  announcement: string;
  connectedCount: number;
}

const SERVER_URL = "ws://localhost:2567";
const CAMERA_HEIGHT = 1.7;
const WORLD_MIN_Z = -1;
const WORLD_MAX_Z = 31;
const WORLD_HALF_WIDTH = 5.4;

const playerMaterialPalette: Record<RoleType, number> = {
  heavy: 0xd58936,
  runner: 0x59c3ff,
};

export class PanicDemoApp {
  private readonly elements: PanicDemoAppElements;
  private readonly renderer: THREE.WebGLRenderer;
  private readonly scene: THREE.Scene;
  private readonly camera: THREE.PerspectiveCamera;
  private readonly localPosition = new THREE.Vector3(0, CAMERA_HEIGHT, 3.5);
  private readonly cargoVisual = new THREE.Group();
  private readonly cargoTarget = new THREE.Vector3(0, 1.2, 4.8);
  private readonly playerMeshes = new Map<string, THREE.Group>();
  private readonly pressedKeys = new Set<string>();
  private readonly rotation = new THREE.Euler(0, 0, 0, "YXZ");
  private readonly pulseMaterial: THREE.ShaderMaterial;
  private readonly dangerMaterial: THREE.ShaderMaterial;
  private readonly deliveryMaterial: THREE.ShaderMaterial;
  private readonly catchMaterial: THREE.ShaderMaterial;

  private localRole: RoleType = "heavy";
  private localReady = false;
  private room: Room | null = null;
  private sessionId = "";
  private snapshot: RoomSnapshot | null = null;
  private sendAccumulator = 0;
  private blanketHeld = false;
  private lastFrameTime = performance.now();
  private elapsedTime = 0;
  private reconnectTimeoutId: number | null = null;
  private hasInitialServerPosition = false;

  constructor(elements: PanicDemoAppElements) {
    this.elements = elements;

    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(elements.viewport.clientWidth, elements.viewport.clientHeight);
    this.elements.viewport.appendChild(this.renderer.domElement);

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x120f16);
    this.scene.fog = new THREE.Fog(0x120f16, 20, 62);

    this.camera = new THREE.PerspectiveCamera(
      75,
      elements.viewport.clientWidth / elements.viewport.clientHeight,
      0.1,
      120,
    );
    this.camera.position.copy(this.localPosition);

    this.pulseMaterial = this.createPulseMaterial();
    this.dangerMaterial = this.createDangerMaterial();
    this.deliveryMaterial = this.createRingMaterial(new THREE.Color(0x5ef0c5));
    this.catchMaterial = this.createRingMaterial(new THREE.Color(0x78b8ff));

    this.buildScene();
    this.bindUi();
    this.connectToRoom().catch((error: unknown) => {
      this.handleConnectionError(error);
    });

    window.addEventListener("resize", () => this.handleResize());
    document.addEventListener("pointerlockchange", () => this.updatePointerStatus());
    document.addEventListener("mousemove", (event) => this.handleMouseMove(event));
    document.addEventListener("keydown", (event) => this.handleKeyDown(event));
    document.addEventListener("keyup", (event) => this.handleKeyUp(event));
    this.elements.viewport.addEventListener("click", () => {
      this.renderer.domElement.requestPointerLock();
    });

    this.animate();
  }

  private bindUi(): void {
    this.elements.connectButton.addEventListener("click", () => {
      this.connectToRoom().catch((error: unknown) => {
        this.handleConnectionError(error);
      });
    });

    this.elements.roleHeavyButton.addEventListener("click", () => this.setRole("heavy"));
    this.elements.roleRunnerButton.addEventListener("click", () => this.setRole("runner"));

    this.elements.readyButton.addEventListener("click", () => {
      this.localReady = !this.localReady;
      this.room?.send("set_ready", { ready: this.localReady });
      this.refreshRoleButtons();
    });

    this.elements.restartButton.addEventListener("click", () => {
      this.room?.send("restart");
    });
  }

  private async connectToRoom(): Promise<void> {
    this.elements.connectionStatus.textContent = "Connecting to local room server...";
    if (this.reconnectTimeoutId !== null) {
      window.clearTimeout(this.reconnectTimeoutId);
      this.reconnectTimeoutId = null;
    }

    if (this.room) {
      await this.room.leave(true);
      this.room = null;
      this.sessionId = "";
      this.hasInitialServerPosition = false;
    }

    const client = new Client(SERVER_URL);
    const room = await client.joinOrCreate("panic_room", {
      name: `Courier-${Math.floor(Math.random() * 900 + 100)}`,
    });

    this.room = room;
    this.sessionId = room.sessionId;
    this.elements.connectionStatus.textContent = `Connected to ${SERVER_URL}`;

    room.onStateChange((state: { toJSON?: () => unknown }) => {
      const snapshot = (state.toJSON ? state.toJSON() : state) as RoomSnapshot;
      this.applySnapshot(snapshot);
    });

    room.onLeave(() => {
      this.room = null;
      this.elements.connectionStatus.textContent = "Disconnected. Attempting to reconnect...";
      this.queueReconnect();
    });
  }

  private applySnapshot(snapshot: RoomSnapshot): void {
    this.snapshot = snapshot;
    this.elements.phaseLabel.textContent = snapshot.phase.toUpperCase();
    this.elements.announcement.textContent = snapshot.announcement;
    this.elements.playerCount.textContent = `Couriers: ${snapshot.connectedCount} / 2`;
    this.updateBars(snapshot.cargo.fragility, snapshot.cargo.stability);

    const localPlayer = snapshot.players[this.sessionId];
    if (localPlayer) {
      this.localRole = localPlayer.role;
      this.localReady = localPlayer.ready;

      const serverPosition = new THREE.Vector3(
        localPlayer.position.x,
        localPlayer.position.y,
        localPlayer.position.z,
      );
      if (!this.hasInitialServerPosition || serverPosition.distanceTo(this.localPosition) > 6) {
        this.localPosition.copy(serverPosition);
        this.hasInitialServerPosition = true;
      }
    }

    this.refreshRoleButtons();
    this.syncPlayerMeshes(snapshot);
    this.syncObjective(snapshot);
  }

  private syncObjective(snapshot: RoomSnapshot): void {
    if (snapshot.phase === "lobby") {
      this.elements.objectiveText.textContent = "Pick Heavy Lifter or Runner, then ready up.";
      return;
    }

    if (snapshot.phase === "completed") {
      this.elements.objectiveText.textContent = `Delivered in ${snapshot.elapsedTime.toFixed(1)}s.`;
      return;
    }

    if (snapshot.phase === "failed") {
      this.elements.objectiveText.textContent = "The cargo failed. Reset and run the route again.";
      return;
    }

    if (snapshot.cargo.position.z < 8.5) {
      this.elements.objectiveText.textContent = "Launch the Runner across the rift, then roll the cargo onto the catapult.";
      return;
    }

    if (snapshot.cargo.airborne || snapshot.cargo.position.z < 23) {
      this.elements.objectiveText.textContent = "Runner: hold Q and catch the launched cargo with the blanket.";
      return;
    }

    this.elements.objectiveText.textContent = "Escort the recovered cargo into the delivery ring.";
  }

  private refreshRoleButtons(): void {
    this.elements.roleHeavyButton.classList.toggle("active", this.localRole === "heavy");
    this.elements.roleRunnerButton.classList.toggle("active", this.localRole === "runner");
    this.elements.readyButton.classList.toggle("active", this.localReady);
    this.elements.readyButton.textContent = this.localReady ? "Cancel Ready" : "Ready Up";
  }

  private setRole(role: RoleType): void {
    this.localRole = role;
    this.room?.send("set_role", { role });
    this.refreshRoleButtons();
  }

  private buildScene(): void {
    const hemiLight = new THREE.HemisphereLight(0xc6cbff, 0x281f1b, 1.3);
    this.scene.add(hemiLight);

    const dirLight = new THREE.DirectionalLight(0xfff0d2, 1.5);
    dirLight.position.set(10, 15, 5);
    this.scene.add(dirLight);

    const ambientDebris = new THREE.Group();
    this.scene.add(ambientDebris);

    this.addGroundSegment(0, 0, 4.5, 12.8, 11.2, 0x3f3746);
    this.addGroundSegment(0, 0, 24.5, 12.8, 13, 0x42384a);

    const rift = new THREE.Mesh(new THREE.BoxGeometry(26, 0.2, 9.5), new THREE.MeshBasicMaterial({ color: 0x050307 }));
    rift.position.set(0, -2.4, 14.5);
    this.scene.add(rift);

    const dangerPlane = new THREE.Mesh(new THREE.PlaneGeometry(16, 10), this.dangerMaterial);
    dangerPlane.rotation.x = -Math.PI / 2;
    dangerPlane.position.set(0, -1.38, 14.5);
    this.scene.add(dangerPlane);

    this.addWall(-6.3, 1, 4.5, 0.24, 2, 5.4, 0x65596a);
    this.addWall(6.3, 1, 4.5, 0.24, 2, 5.4, 0x65596a);
    this.addWall(-6.3, 1, 24.5, 0.24, 2, 6.5, 0x65596a);
    this.addWall(6.3, 1, 24.5, 0.24, 2, 6.5, 0x65596a);

    const catapultBase = new THREE.Mesh(
      new THREE.BoxGeometry(2.2, 0.25, 1.8),
      new THREE.MeshStandardMaterial({ color: 0x4f3430, roughness: 0.85 }),
    );
    catapultBase.position.set(0, 0.18, 7.5);
    this.scene.add(catapultBase);

    const catapultArm = new THREE.Mesh(
      new THREE.BoxGeometry(0.25, 0.25, 2.2),
      new THREE.MeshStandardMaterial({ color: 0x8f6745, roughness: 0.5 }),
    );
    catapultArm.position.set(0, 1.05, 7.2);
    catapultArm.rotation.x = -0.45;
    this.scene.add(catapultArm);

    const catchRing = new THREE.Mesh(new THREE.RingGeometry(1.7, 2.8, 48), this.catchMaterial);
    catchRing.rotation.x = -Math.PI / 2;
    catchRing.position.set(0, 0.03, 21.5);
    this.scene.add(catchRing);

    const deliveryRing = new THREE.Mesh(new THREE.RingGeometry(1.7, 2.9, 48), this.deliveryMaterial);
    deliveryRing.rotation.x = -Math.PI / 2;
    deliveryRing.position.set(0, 0.03, 28);
    this.scene.add(deliveryRing);

    const cargoCore = new THREE.Mesh(
      new THREE.BoxGeometry(1.1, 1.1, 1.1),
      new THREE.MeshStandardMaterial({ color: 0xc68963, roughness: 0.35, metalness: 0.08 }),
    );
    const cargoShell = new THREE.Mesh(new THREE.SphereGeometry(0.95, 18, 18), this.pulseMaterial);
    cargoShell.renderOrder = 1;

    const cartBase = new THREE.Mesh(
      new THREE.BoxGeometry(1.5, 0.12, 1.3),
      new THREE.MeshStandardMaterial({ color: 0x3d464d, roughness: 0.6 }),
    );
    cartBase.position.y = -0.72;
    cartBase.visible = false;

    for (const wheelX of [-0.5, 0.5]) {
      for (const wheelZ of [-0.4, 0.4]) {
        const wheel = new THREE.Mesh(
          new THREE.CylinderGeometry(0.14, 0.14, 0.08, 16),
          new THREE.MeshStandardMaterial({ color: 0x111216, roughness: 0.8 }),
        );
        wheel.rotation.z = Math.PI / 2;
        wheel.position.set(wheelX, -0.82, wheelZ);
        wheel.visible = false;
        this.cargoVisual.add(wheel);
      }
    }

    this.cargoVisual.add(cargoCore, cargoShell, cartBase);
    this.cargoVisual.userData.cartBase = cartBase;
    this.scene.add(this.cargoVisual);

    for (let index = 0; index < 22; index += 1) {
      const block = new THREE.Mesh(
        new THREE.BoxGeometry(1 + Math.random() * 1.8, 0.8 + Math.random() * 1.4, 1 + Math.random() * 1.8),
        new THREE.MeshStandardMaterial({ color: 0x2a242f, roughness: 0.95 }),
      );
      const side = index % 2 === 0 ? -1 : 1;
      const z = index < 11 ? index * 0.8 : 20 + (index - 11) * 0.9;
      block.position.set(side * (7 + Math.random() * 4.5), 0.35 + Math.random() * 1.1, z);
      ambientDebris.add(block);
    }
  }

  private addGroundSegment(x: number, y: number, z: number, width: number, depth: number, color: number): void {
    const mesh = new THREE.Mesh(
      new THREE.BoxGeometry(width, 0.45, depth),
      new THREE.MeshStandardMaterial({ color, roughness: 0.93 }),
    );
    mesh.position.set(x, y - 0.22, z);
    this.scene.add(mesh);
  }

  private addWall(
    x: number,
    y: number,
    z: number,
    width: number,
    height: number,
    depth: number,
    color: number,
  ): void {
    const mesh = new THREE.Mesh(
      new THREE.BoxGeometry(width, height, depth),
      new THREE.MeshStandardMaterial({ color, roughness: 0.82 }),
    );
    mesh.position.set(x, y, z);
    this.scene.add(mesh);
  }

  private syncPlayerMeshes(snapshot: RoomSnapshot): void {
    const activeIds = new Set(Object.keys(snapshot.players));
    for (const [sessionId, mesh] of this.playerMeshes.entries()) {
      if (!activeIds.has(sessionId)) {
        this.scene.remove(mesh);
        this.playerMeshes.delete(sessionId);
      }
    }

    for (const player of Object.values(snapshot.players)) {
      if (player.sessionId === this.sessionId) {
        continue;
      }

      let mesh = this.playerMeshes.get(player.sessionId);
      if (!mesh) {
        mesh = this.createPlayerMesh(player.role);
        this.playerMeshes.set(player.sessionId, mesh);
        this.scene.add(mesh);
      }

      const bodyMaterial = mesh.userData.bodyMaterial as THREE.MeshStandardMaterial | undefined;
      if (bodyMaterial) {
        bodyMaterial.color.setHex(playerMaterialPalette[player.role]);
        bodyMaterial.emissive.setHex(player.blanketActive ? 0x24435a : 0x080808);
      }

      mesh.position.set(player.position.x, 0, player.position.z);
      mesh.rotation.y = player.yaw;
    }
  }

  private createPlayerMesh(role: RoleType): THREE.Group {
    const group = new THREE.Group();
    const bodyMaterial = new THREE.MeshStandardMaterial({
      color: playerMaterialPalette[role],
      roughness: 0.45,
      metalness: 0.05,
    });
    const headMaterial = new THREE.MeshStandardMaterial({ color: 0xf0d7b8, roughness: 0.6 });

    const bodyRadius = role === "heavy" ? 0.34 : 0.26;
    const bodyHeight = role === "heavy" ? 1.08 : 0.94;
    const body = new THREE.Mesh(new THREE.CapsuleGeometry(bodyRadius, bodyHeight, 8, 12), bodyMaterial);
    body.position.y = role === "heavy" ? 1.0 : 0.96;
    group.add(body);

    const head = new THREE.Mesh(new THREE.SphereGeometry(0.24, 16, 16), headMaterial);
    head.position.y = 1.92;
    group.add(head);

    const visor = new THREE.Mesh(
      new THREE.BoxGeometry(0.28, 0.14, 0.04),
      new THREE.MeshStandardMaterial({ color: 0x1b2430, emissive: 0x233449, emissiveIntensity: 0.7 }),
    );
    visor.position.set(0, 1.92, 0.23);
    group.add(visor);

    const pack = new THREE.Mesh(
      new THREE.BoxGeometry(role === "heavy" ? 0.32 : 0.22, 0.44, 0.18),
      new THREE.MeshStandardMaterial({ color: 0x1a1a1f, roughness: 0.8 }),
    );
    pack.position.set(0, 1.08, -0.28);
    group.add(pack);

    group.userData.bodyMaterial = bodyMaterial;
    return group;
  }

  private animate = (): void => {
    requestAnimationFrame(this.animate);

    const now = performance.now();
    const deltaTime = Math.min((now - this.lastFrameTime) / 1000, 0.1);
    this.lastFrameTime = now;
    this.elapsedTime += deltaTime;
    this.updateLocalMovement(deltaTime);
    this.updateCamera();
    this.updateWorldVisuals();
    this.renderer.render(this.scene, this.camera);
  };

  private updateLocalMovement(deltaTime: number): void {
    const forward = new THREE.Vector3(-Math.sin(this.rotation.y), 0, -Math.cos(this.rotation.y));
    const right = new THREE.Vector3(Math.cos(this.rotation.y), 0, -Math.sin(this.rotation.y));
    const move = new THREE.Vector3();

    if (this.pressedKeys.has("KeyW")) move.add(forward);
    if (this.pressedKeys.has("KeyS")) move.sub(forward);
    if (this.pressedKeys.has("KeyD")) move.add(right);
    if (this.pressedKeys.has("KeyA")) move.sub(right);

    const carryingCargo = this.snapshot?.cargo.carrierId === this.sessionId;
    const baseSpeed = this.localRole === "heavy" ? 4 : 5.5;
    const moveSpeed = carryingCargo ? baseSpeed * 0.72 : baseSpeed;

    if (move.lengthSq() > 0) {
      move.normalize().multiplyScalar(moveSpeed * deltaTime);
      this.localPosition.add(move);
    }

    this.localPosition.z = THREE.MathUtils.clamp(this.localPosition.z, WORLD_MIN_Z, WORLD_MAX_Z);
    this.localPosition.x = THREE.MathUtils.clamp(this.localPosition.x, -WORLD_HALF_WIDTH, WORLD_HALF_WIDTH);
    this.localPosition.y = CAMERA_HEIGHT;

    if (this.room) {
      this.sendAccumulator += deltaTime;
      if (this.sendAccumulator >= 1 / 20) {
        this.sendAccumulator = 0;
        this.room.send("player_update", {
          position: {
            x: this.localPosition.x,
            y: this.localPosition.y,
            z: this.localPosition.z,
          },
          yaw: this.rotation.y,
          blanketActive: this.blanketHeld,
        });
      }
    }
  }

  private updateCamera(): void {
    this.camera.position.copy(this.localPosition);
    this.camera.rotation.copy(this.rotation);
  }

  private updateWorldVisuals(): void {
    const elapsed = this.elapsedTime;
    this.pulseMaterial.uniforms.uTime.value = elapsed;
    this.dangerMaterial.uniforms.uTime.value = elapsed;
    this.deliveryMaterial.uniforms.uTime.value = elapsed;
    this.catchMaterial.uniforms.uTime.value = elapsed;

    if (!this.snapshot) {
      return;
    }

    this.cargoTarget.set(
      this.snapshot.cargo.position.x,
      this.snapshot.cargo.position.y,
      this.snapshot.cargo.position.z,
    );
    this.cargoVisual.position.lerp(this.cargoTarget, 0.22);

    const auraColor = this.snapshot.cargo.panic ? new THREE.Color(0xff5370) : new THREE.Color(0x5be7ff);
    this.pulseMaterial.uniforms.uPulseColor.value.copy(auraColor);
    this.pulseMaterial.uniforms.uPulseStrength.value = this.snapshot.cargo.airborne ? 1.1 : this.snapshot.cargo.panic ? 1.3 : 0.55;

    const cartBase = this.cargoVisual.userData.cartBase as THREE.Mesh | undefined;
    if (cartBase) {
      cartBase.visible = this.snapshot.cargo.cartLatched;
    }

    this.cargoVisual.children.forEach((child: THREE.Object3D, index: number) => {
      if (index >= 3) {
        child.visible = this.snapshot?.cargo.cartLatched ?? false;
      }
    });
  }

  private updateBars(fragility: number, stability: number): void {
    const clampedFragility = THREE.MathUtils.clamp(fragility, 0, 100);
    const clampedStability = THREE.MathUtils.clamp(stability, 0, 100);

    this.elements.fragilityBar.style.width = `${clampedFragility}%`;
    this.elements.stabilityBar.style.width = `${clampedStability}%`;
    this.elements.fragilityValue.textContent = `${clampedFragility.toFixed(0)}%`;
    this.elements.stabilityValue.textContent = `${clampedStability.toFixed(0)}%`;
  }

  private handleResize(): void {
    const { clientWidth, clientHeight } = this.elements.viewport;
    this.camera.aspect = clientWidth / clientHeight;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(clientWidth, clientHeight);
  }

  private updatePointerStatus(): void {
    const locked = document.pointerLockElement === this.renderer.domElement;
    this.elements.connectionStatus.dataset.locked = locked ? "true" : "false";
  }

  private handleMouseMove(event: MouseEvent): void {
    if (document.pointerLockElement !== this.renderer.domElement) {
      return;
    }

    this.rotation.y -= event.movementX * 0.0025;
    this.rotation.x -= event.movementY * 0.0022;
    this.rotation.x = THREE.MathUtils.clamp(this.rotation.x, -1.2, 1.2);
  }

  private handleKeyDown(event: KeyboardEvent): void {
    if (["KeyW", "KeyA", "KeyS", "KeyD"].includes(event.code)) {
      this.pressedKeys.add(event.code);
    }

    if (event.code === "KeyQ") {
      this.blanketHeld = true;
    }

    if (event.repeat) {
      return;
    }

    if (event.code === "KeyF") {
      this.room?.send("interact");
    }

    if (event.code === "KeyE") {
      this.room?.send("use_catapult");
    }

    if (event.code === "KeyR") {
      this.room?.send("toggle_cart");
    }
  }

  private handleKeyUp(event: KeyboardEvent): void {
    this.pressedKeys.delete(event.code);
    if (event.code === "KeyQ") {
      this.blanketHeld = false;
    }
  }

  private handleConnectionError(error: unknown): void {
    this.elements.connectionStatus.textContent = "Connection failed. Retrying shortly...";
    this.queueReconnect();
    console.error(error);
  }

  private queueReconnect(): void {
    if (this.reconnectTimeoutId !== null) {
      return;
    }

    this.reconnectTimeoutId = window.setTimeout(() => {
      this.reconnectTimeoutId = null;
      this.connectToRoom().catch((error: unknown) => {
        this.handleConnectionError(error);
      });
    }, 1500);
  }

  private createPulseMaterial(): THREE.ShaderMaterial {
    return new THREE.ShaderMaterial({
      transparent: true,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      uniforms: {
        uTime: { value: 0 },
        uPulseColor: { value: new THREE.Color(0x5be7ff) },
        uPulseStrength: { value: 0.55 },
      },
      vertexShader: `
        varying vec3 vNormal;
        varying vec3 vWorldPosition;
        void main() {
          vNormal = normalize(normalMatrix * normal);
          vec4 worldPosition = modelMatrix * vec4(position, 1.0);
          vWorldPosition = worldPosition.xyz;
          gl_Position = projectionMatrix * viewMatrix * worldPosition;
        }
      `,
      fragmentShader: `
        uniform float uTime;
        uniform vec3 uPulseColor;
        uniform float uPulseStrength;
        varying vec3 vNormal;
        varying vec3 vWorldPosition;
        void main() {
          float fresnel = pow(1.0 - abs(dot(vNormal, vec3(0.0, 0.0, 1.0))), 2.0);
          float pulse = 0.45 + 0.55 * sin(uTime * 4.0 + vWorldPosition.y * 3.0);
          float alpha = fresnel * pulse * uPulseStrength;
          gl_FragColor = vec4(uPulseColor, alpha);
        }
      `,
    });
  }

  private createDangerMaterial(): THREE.ShaderMaterial {
    return new THREE.ShaderMaterial({
      transparent: true,
      side: THREE.DoubleSide,
      uniforms: { uTime: { value: 0 } },
      vertexShader: `
        varying vec2 vUv;
        void main() {
          vUv = uv;
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform float uTime;
        varying vec2 vUv;
        void main() {
          float bands = smoothstep(0.18, 0.82, sin((vUv.y * 16.0) - (uTime * 2.3)) * 0.5 + 0.5);
          vec3 base = mix(vec3(0.04, 0.02, 0.05), vec3(0.62, 0.16, 0.22), bands);
          gl_FragColor = vec4(base, 0.34 + bands * 0.28);
        }
      `,
    });
  }

  private createRingMaterial(color: THREE.Color): THREE.ShaderMaterial {
    return new THREE.ShaderMaterial({
      transparent: true,
      side: THREE.DoubleSide,
      uniforms: {
        uTime: { value: 0 },
        uColor: { value: color },
      },
      vertexShader: `
        varying vec2 vUv;
        void main() {
          vUv = uv;
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform float uTime;
        uniform vec3 uColor;
        varying vec2 vUv;
        void main() {
          float pulse = 0.55 + 0.45 * sin(uTime * 3.2);
          gl_FragColor = vec4(uColor * pulse, 0.42);
        }
      `,
    });
  }
}
