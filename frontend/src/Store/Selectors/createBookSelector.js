import { createSelector } from 'reselect';

function createBookSelector() {
  return createSelector(
    (state, { bookId }) => bookId,
    (state) => state.books.itemMap,
    (state) => state.books.items,
    (bookId, itemMap, allBooks) => {
      if (!bookId || !itemMap || !allBooks) {
        return null;
      }

      const index = itemMap[bookId];
      if (index == null || index < 0 || index >= allBooks.length) {
        return null;
      }

      return allBooks[index];
    }
  );
}

export default createBookSelector;
