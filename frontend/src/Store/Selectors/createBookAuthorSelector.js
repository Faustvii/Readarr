import { createSelector } from 'reselect';
import createBookSelector from './createBookSelector';

function createBookAuthorSelector() {
  return createSelector(
    createBookSelector(),
    (state) => state.authors.itemMap,
    (state) => state.authors.items,
    (book, authorMap, allAuthors) => {
      if (!book || !book.authorId) {
        return null;
      }
      return allAuthors[authorMap[book.authorId]];
    }
  );
}

export default createBookAuthorSelector;
