import { createSelector } from 'reselect';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createAuthorClientSideCollectionItemsSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('authors', uiSection),
    (state) => state.books.items,
    (authors, books) => {
      const booksByAuthor = books.reduce((acc, book) => {
        if (!acc[book.authorId]) {
          acc[book.authorId] = [];
        }
        acc[book.authorId].push(book);
        return acc;
      }, {});

      const items = authors.items.map((s) => {
        const {
          id,
          sortName,
          sortNameLastFirst,
          authorName,
          monitored,
          status,
          isSaving,
          statistics
        } = s;

        const authorBooks = booksByAuthor[id] || [];

        return {
          id,
          sortName,
          sortNameLastFirst,
          authorName,
          monitored,
          status,
          isSaving,
          statistics,
          books: authorBooks
        };
      });

      return {
        ...authors,
        items
      };
    }
  );
}

export default createAuthorClientSideCollectionItemsSelector;
