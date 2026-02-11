import {
  combineDateAndTime,
  isValidTime,
  parseIsoDate,
  parseTime,
  toDateKey,
  toIsoDate,
  toTimeInputValue,
  toTimeString
} from './date-time.utils';

describe('date-time.utils', () => {
  it('should format Date to yyyy-mm-dd', () => {
    const value = new Date(2026, 1, 11);
    expect(toIsoDate(value)).toBe('2026-02-11');
  });

  it('should parse yyyy-mm-dd without shifting the day', () => {
    const value = parseIsoDate('2026-02-11');
    expect(value).not.toBeNull();
    expect(value!.getFullYear()).toBe(2026);
    expect(value!.getMonth()).toBe(1);
    expect(value!.getDate()).toBe(11);
  });

  it('should convert HH:mm string to Date and back', () => {
    const parsed = parseTime('09:45');
    expect(parsed).not.toBeNull();
    expect(toTimeString(parsed)).toBe('09:45');
  });

  it('should combine date and time into a Date instance', () => {
    const combined = combineDateAndTime(new Date(2026, 1, 11), '13:20');
    expect(toDateKey(combined)).toBe('2026-02-11');
    expect(toTimeString(combined)).toBe('13:20');
  });

  it('should produce time input value in HH:mm', () => {
    const value = new Date(2026, 1, 11, 7, 5);
    expect(toTimeInputValue(value)).toBe('07:05');
  });

  it('should validate time format', () => {
    expect(isValidTime('18:30')).toBeTrue();
    expect(isValidTime('25:99')).toBeFalse();
  });
});

