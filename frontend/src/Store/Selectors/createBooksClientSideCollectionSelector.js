import _ from 'lodash';
import { createSelector } from 'reselect';
import filterCollection from 'Utilities/Array/filterCollection';
import sortCollection from 'Utilities/Array/sortCollection';
import createCustomFiltersSelector from './createCustomFiltersSelector';

function createBooksClientSideCollectionSelector(uiSection) {
  let lastSortKey = null;
  let lastSortDirection = null;

  return createSelector(
    (state) => _.get(state, 'books'),
    (state) => _.get(state, 'authors'),
    (state) => _.get(state, uiSection),
    createCustomFiltersSelector('books', uiSection),
    (bookState, authorState, uiSectionState = {}, customFilters) => {
      const state = Object.assign({}, bookState, uiSectionState, { customFilters });

      const books = state.items;
      for (const book of books) {
        if (book && book.authorId && authorState.itemMap && authorState.items) {
          const authorIndex = authorState.itemMap[book.authorId];
          if (authorIndex != null && authorIndex >= 0 && authorIndex < authorState.items.length) {
            book.author = authorState.items[authorIndex];
          }
        }
      }

      const filtered = filterCollection(books, state);

      const currentSortKey = state.sortKey || 'releaseDate';
      const currentSortDirection = state.sortDirection || 'descending';

      const sortChanged = lastSortKey !== currentSortKey || lastSortDirection !== currentSortDirection;

      if (sortChanged) {
        lastSortKey = currentSortKey;
        lastSortDirection = currentSortDirection;

        const sorted = sortCollection(filtered, state);

        return {
          ...bookState,
          ...uiSectionState,
          customFilters,
          items: sorted,
          totalItems: state.items.length
        };
      }

      return {
        ...bookState,
        ...uiSectionState,
        customFilters,
        items: filtered,
        totalItems: state.items.length
      };

    }
  );
}

function createAuthorBooksSelector(authorId) {
  return createSelector(
    (state) => _.get(state, 'books'),
    (state) => _.get(state, 'authors'),
    (bookState, authorState) => {
      const books = bookState.items;

      const authorBooks = authorId ? books.filter((book) => book.authorId === authorId) : books;

      for (const book of authorBooks) {
        if (book && book.authorId && authorState.itemMap && authorState.items) {
          const authorIndex = authorState.itemMap[book.authorId];
          if (authorIndex != null && authorIndex >= 0 && authorIndex < authorState.items.length) {
            book.author = authorState.items[authorIndex];
          }
        }
      }

      return {
        ...bookState,
        items: authorBooks,
        totalItems: authorBooks.length
      };
    }
  );
}

export default createBooksClientSideCollectionSelector;
export { createAuthorBooksSelector };
