import {
  createHash,
  pbkdf2Sync,
  randomBytes,
  scryptSync,
  timingSafeEqual,
} from 'node:crypto';

export function hashPassword(password: string): string {
  const salt = randomBytes(16);
  const key = scryptSync(password, salt, 64);
  return `scrypt$${salt.toString('base64')}$${key.toString('base64')}`;
}

export function verifyPassword(password: string, stored: string): boolean {
  if (stored.startsWith('scrypt$')) {
    const parts = stored.split('$');
    if (parts.length !== 3) return false;
    const salt = Buffer.from(parts[1], 'base64');
    const expected = Buffer.from(parts[2], 'base64');
    const actual = scryptSync(password, salt, expected.length);
    return expected.length === actual.length && timingSafeEqual(expected, actual);
  }

  const data = Buffer.from(stored, 'base64');
  if (data.length < 14 || data[0] !== 1) return false;
  const prf = data.readUInt32BE(1);
  const iterations = data.readUInt32BE(5);
  const saltLength = data.readUInt32BE(9);
  if (saltLength < 16 || iterations < 1 || data.length < 13 + saltLength + 16) {
    return false;
  }

  const algorithm = prf === 0 ? 'sha1' : prf === 1 ? 'sha256' : prf === 2 ? 'sha512' : null;
  if (!algorithm) return false;
  const salt = data.subarray(13, 13 + saltLength);
  const expected = data.subarray(13 + saltLength);
  const actual = pbkdf2Sync(password, salt, iterations, expected.length, algorithm);
  return timingSafeEqual(expected, actual);
}

export function sha256(value: string): string {
  return createHash('sha256').update(value).digest('hex');
}
