import {
  FACE_FRAMES,
  isInLayer,
  quarterTurns,
  rotateClockwise,
  stickerAt,
  stickerPosition,
  turnAngle,
  type Vec3,
} from './geometry';
import { FACES } from './types';

// Rotation can produce -0, which toEqual treats as different from 0.
const normalise = (v: Vec3) => v.map((n) => n + 0);

const ALL_STICKERS = FACES.flatMap((face) =>
  Array.from({ length: 9 }, (_, index) => ({ face, index })),
);

describe('geometry', () => {
  it('places every sticker on a distinct cubie face and finds it again', () => {
    const seen = new Set<string>();
    for (const { face, index } of ALL_STICKERS) {
      const position = stickerPosition(face, index);
      seen.add(`${position.join(',')}|${face}`);
      expect(stickerAt(position, FACE_FRAMES[face].normal)).toEqual({ face, index });
    }
    expect(seen.size).toBe(54);
  });

  it('puts centres on the face normal and corners on the cube corners', () => {
    expect(stickerPosition('F', 4)).toEqual([0, 0, 1]);
    expect(stickerPosition('U', 0)).toEqual([-1, 1, -1]);
    expect(stickerPosition('D', 8)).toEqual([1, -1, -1]);
  });

  it('rejects a direction that is not a face normal', () => {
    expect(() => stickerAt([1, 1, 1], [1, 1, 0])).toThrow('No face has normal 1,1,0.');
  });

  it('turns clockwise as seen from outside the face', () => {
    // Looking at F: top-centre goes to right-centre.
    expect(rotateClockwise([0, 1, 1], FACE_FRAMES.F.normal)).toEqual([1, 0, 1]);
    // Looking at U: back-centre goes to right-centre.
    expect(rotateClockwise([0, 1, -1], FACE_FRAMES.U.normal)).toEqual([1, 1, 0]);
  });

  it('returns to the start after four quarter turns about any face', () => {
    const start: Vec3 = [1, -1, 0];
    for (const face of FACES) {
      let v = start;
      for (let i = 0; i < 4; i++) v = rotateClockwise(v, FACE_FRAMES[face].normal);
      expect(normalise(v)).toEqual(start);
    }
  });

  it('puts nine cubies in each turned layer', () => {
    const cubies = [-1, 0, 1].flatMap((x) =>
      [-1, 0, 1].flatMap((y) => [-1, 0, 1].map((z): Vec3 => [x, y, z])),
    );
    for (const face of FACES) {
      expect(cubies.filter((c) => isInLayer(c, face))).toHaveLength(9);
    }
  });

  it('maps turns to quarter turns and angles', () => {
    expect(quarterTurns('clockwise')).toBe(1);
    expect(quarterTurns('half')).toBe(2);
    expect(quarterTurns('antiClockwise')).toBe(3);
    expect(turnAngle('clockwise')).toBeCloseTo(-Math.PI / 2);
    expect(turnAngle('antiClockwise')).toBeCloseTo(Math.PI / 2);
    expect(turnAngle('half')).toBeCloseTo(-Math.PI);
  });
});
