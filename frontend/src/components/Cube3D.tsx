import { OrbitControls, RoundedBox } from '@react-three/drei';
import { Canvas, useFrame, useThree } from '@react-three/fiber';
import { useImperativeHandle, useMemo, useRef, type Ref } from 'react';
import { Spherical, Vector3, type Group } from 'three';
import { FACE_FRAMES, isInLayer, stickerAt, turnAngle, type Vec3 } from '../cube/geometry';
import { COLOUR_HEX, FACES, type CubeState, type Face } from '../cube/types';
import type { Animation } from '../session/CubeSession';

export type ViewDirection = 'left' | 'right' | 'up' | 'down';

export interface Cube3DHandle {
  rotateView(direction: ViewDirection): void;
  resetView(): void;
}

interface Cube3DProps {
  state: CubeState;
  animation: Animation | null;
  ref?: Ref<Cube3DHandle>;
}

/** Front-right-up view, as in the brief's picture: Green front, Red right, White up. */
/** The parts of drei's OrbitControls the view buttons use. */
interface OrbitControlsLike {
  target: Vector3;
  update(): void;
}

const DEFAULT_CAMERA: Vec3 = [4.2, 3.6, 6.2];
const VIEW_STEP = Math.PI / 8;
const MIN_POLAR = 0.15;
const CUBIE_SIZE = 0.96;
const STICKER_SIZE = 0.8;
const STICKER_OFFSET = CUBIE_SIZE / 2 + 0.002;
const BODY_COLOUR = '#151515';

const CUBIES: Vec3[] = [-1, 0, 1]
  .flatMap((x) => [-1, 0, 1].flatMap((y) => [-1, 0, 1].map((z): Vec3 => [x, y, z])))
  .filter(([x, y, z]) => x !== 0 || y !== 0 || z !== 0);

/** Rotations that turn a +z-facing plane to face each direction. */
const STICKER_ROTATION: Record<Face, Vec3> = {
  U: [-Math.PI / 2, 0, 0],
  D: [Math.PI / 2, 0, 0],
  F: [0, 0, 0],
  B: [0, Math.PI, 0],
  R: [0, Math.PI / 2, 0],
  L: [0, -Math.PI / 2, 0],
};

/** The interactive 3D cube: drag to orbit, buttons rotate the camera, never the cube's state. */
export function Cube3D({ state, animation, ref }: Cube3DProps) {
  return (
    <div
      className="cube3d"
      role="img"
      aria-label="3D view of the cube. Drag to look around; the net view lists every sticker."
    >
      <Canvas camera={{ position: DEFAULT_CAMERA, fov: 35 }} dpr={[1, 2]}>
        <ambientLight intensity={1.5} />
        <directionalLight position={[5, 8, 6]} intensity={1.5} />
        <CubeModel state={state} animation={animation} />
        <CameraRig ref={ref} />
      </Canvas>
    </div>
  );
}

function CameraRig({ ref }: { ref?: Ref<Cube3DHandle> }) {
  const camera = useThree((three) => three.camera);
  const controls = useThree((three) => three.controls) as unknown as OrbitControlsLike | null;

  useImperativeHandle(
    ref,
    () => ({
      rotateView(direction) {
        const target = controls?.target ?? new Vector3();
        const offset = camera.position.clone().sub(target);
        const spherical = new Spherical().setFromVector3(offset);
        if (direction === 'left') spherical.theta += VIEW_STEP;
        if (direction === 'right') spherical.theta -= VIEW_STEP;
        if (direction === 'up') spherical.phi += VIEW_STEP;
        if (direction === 'down') spherical.phi -= VIEW_STEP;
        spherical.phi = Math.min(Math.max(spherical.phi, MIN_POLAR), Math.PI - MIN_POLAR);
        camera.position.copy(target).add(offset.setFromSpherical(spherical));
        camera.lookAt(target);
        controls?.update();
      },
      resetView() {
        controls?.target.set(0, 0, 0);
        camera.position.set(...DEFAULT_CAMERA);
        camera.lookAt(0, 0, 0);
        controls?.update();
      },
    }),
    [camera, controls],
  );

  return <OrbitControls makeDefault enablePan={false} minDistance={5} maxDistance={14} />;
}

function CubeModel({ state, animation }: { state: CubeState; animation: Animation | null }) {
  const layer = useRef<Group>(null);
  const face = animation?.move.face;
  const moving = face ? CUBIES.filter((p) => isInLayer(p, face)) : [];
  const still = face ? CUBIES.filter((p) => !isInLayer(p, face)) : CUBIES;

  const axis = useMemo(
    () => (face ? new Vector3(...FACE_FRAMES[face].normal) : new Vector3()),
    [face],
  );

  useFrame(() => {
    if (!layer.current || !animation) return;
    const elapsed = performance.now() - animation.startedAt;
    const progress = animation.durationMs > 0 ? Math.min(elapsed / animation.durationMs, 1) : 1;
    layer.current.quaternion.setFromAxisAngle(
      axis,
      turnAngle(animation.move.turn) * easeInOut(progress),
    );
  });

  return (
    <group>
      {still.map((position) => (
        <Cubie key={position.join()} position={position} state={state} />
      ))}
      {/* Keyed by animation so each move starts from an unrotated layer. */}
      <group ref={layer} key={animation?.id ?? 'idle'}>
        {moving.map((position) => (
          <Cubie key={position.join()} position={position} state={state} />
        ))}
      </group>
    </group>
  );
}

function Cubie({ position, state }: { position: Vec3; state: CubeState }) {
  const outward = FACES.filter((face) => isInLayer(position, face));
  return (
    <group position={position}>
      <RoundedBox args={[CUBIE_SIZE, CUBIE_SIZE, CUBIE_SIZE]} radius={0.07} smoothness={3}>
        <meshStandardMaterial color={BODY_COLOUR} roughness={0.6} />
      </RoundedBox>
      {outward.map((face) => {
        const normal = FACE_FRAMES[face].normal;
        const { index } = stickerAt(position, normal);
        return (
          <mesh
            key={face}
            position={[
              normal[0] * STICKER_OFFSET,
              normal[1] * STICKER_OFFSET,
              normal[2] * STICKER_OFFSET,
            ]}
            rotation={STICKER_ROTATION[face]}
          >
            <planeGeometry args={[STICKER_SIZE, STICKER_SIZE]} />
            <meshBasicMaterial color={COLOUR_HEX[state.faces[face][index]]} toneMapped={false} />
          </mesh>
        );
      })}
    </group>
  );
}

function easeInOut(t: number): number {
  return t < 0.5 ? 2 * t * t : 1 - (-2 * t + 2) ** 2 / 2;
}
