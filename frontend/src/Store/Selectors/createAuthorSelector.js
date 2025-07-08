import { createSelector } from 'reselect';

function createAuthorSelector() {
  return createSelector(
    (state, { authorId }) => authorId,
    (state) => state.authors.itemMap,
    (state) => state.authors.items,
    (authorId, itemMap, allAuthors) => {
      if (!authorId || !itemMap || !allAuthors) {
        return null;
      }

      const index = itemMap[authorId];
      if (index == null || index < 0 || index >= allAuthors.length) {
        return null;
      }

      return allAuthors[index];
    }
  );
}

export default createAuthorSelector;
