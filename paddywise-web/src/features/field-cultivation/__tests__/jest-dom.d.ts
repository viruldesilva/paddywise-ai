// Types for jest-dom's matchers (toBeInTheDocument, toHaveTextContent, …) on Vitest's
// `expect`. src/test/setup.ts registers the matchers at run time; this makes `tsc -b`
// (which type-checks all of src) aware of them.
import '@testing-library/jest-dom/vitest';
