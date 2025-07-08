import _ from 'lodash';
import moment from 'moment';
import React from 'react';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import bookEntities from 'Book/bookEntities';
import Icon from 'Components/Icon';
import { filterTypePredicates, filterTypes, icons, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import translate from 'Utilities/String/translate';
import { removeItem, set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createClearReducer from './Creators/Reducers/createClearReducer';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'books';

export const filters = [
  {
    key: 'all',
    label: () => translate('All'),
    filters: []
  },
  {
    key: 'monitored',
    label: () => translate('Monitored'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'unmonitored',
    label: () => translate('Unmonitored'),
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: () => translate('Missing'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'wanted',
    label: () => translate('Wanted'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'releaseDate',
        value: moment(),
        type: filterTypes.LESS_THAN
      }
    ]
  }
];

export const filterPredicates = {
  missing: function(item) {
    const { statistics = {} } = item;

    return !statistics.hasOwnProperty('bookFileCount') || statistics.bookFileCount === 0;
  },

  releaseDate: function(item, filterValue, type) {
    return dateFilterPredicate(item.releaseDate, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  qualityProfileId: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.author.qualityProfileId, filterValue);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  path: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.author.path, filterValue);
  },

  bookFileCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const bookCount = item.statistics ? item.statistics.bookFileCount : 0;

    return predicate(bookCount, filterValue);
  },

  sizeOnDisk: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const sizeOnDisk = item.statistics && item.statistics.sizeOnDisk ?
      item.statistics.sizeOnDisk :
      0;

    return predicate(sizeOnDisk, filterValue);
  }
};

export const sortPredicates = {
  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  },

  path: function(item) {
    return item.author.path;
  },

  series: function(item) {
    return item.seriesTitle;
  },

  rating: function(item) {
    return item.ratings.value;
  },

  status: function(item) {
    let result = 0;

    const hasBookFile = !!item.statistics?.bookFileCount;
    const isAvailable = Date.parse(item.releaseDate) < new Date();

    if (isAvailable) {
      result++;
    }

    if (item.monitored) {
      result += 2;
    }

    if (hasBookFile) {
      result += 4;
    }

    return result;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  items: [],
  pendingChanges: {},
  pageSize: 50,
  totalPages: 0,
  totalRecords: 0,
  page: 1,
  sortPredicates: {
    rating: function(item) {
      return item.ratings.value;
    }
  },

  columns: [
    {
      name: 'select',
      columnLabel: 'Select',
      isSortable: false,
      isVisible: true,
      isModifiable: false,
      isHidden: true
    },
    {
      name: 'monitored',
      columnLabel: 'Monitored',
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'title',
      label: 'Title',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'series',
      label: 'Series',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'pageCount',
      label: 'Pages',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'rating',
      label: 'Rating',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'indexerFlags',
      columnLabel: () => translate('IndexerFlags'),
      label: React.createElement(Icon, {
        name: icons.FLAG,
        title: () => translate('IndexerFlags')
      }),
      isVisible: false
    },
    {
      name: 'status',
      label: 'Status',
      isVisible: true,
      isSortable: true
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ]
};

export const persistState = [
  'books.pageSize',
  'books.sortKey',
  'books.sortDirection',
  'books.selectedFilterKey',
  'books.columns'
];

//
// Action Types

export const FETCH_BOOKS = 'books/fetchBooks';
export const FETCH_BOOKS_BY_AUTHOR = 'books/fetchBooksByAuthor';
export const FETCH_BOOKS_BY_IDS = 'books/fetchBooksByIds';
export const FETCH_BOOK_BY_SLUG = 'books/fetchBookBySlug';
export const FETCH_BOOKS_SCHEMA = 'books/fetchBooksSchema';
export const GOTO_BOOKS_FIRST_PAGE = 'books/gotoBooksFirstPage';
export const GOTO_BOOKS_PREVIOUS_PAGE = 'books/gotoBooksPreviousPage';
export const GOTO_BOOKS_NEXT_PAGE = 'books/gotoBooksNextPage';
export const GOTO_BOOKS_LAST_PAGE = 'books/gotoBooksLastPage';
export const GOTO_BOOKS_PAGE = 'books/gotoBooksPage';
export const FETCH_BOOKS_NEXT_PAGE = 'books/fetchBooksNextPage';
export const SET_BOOKS_SORT = 'books/setBooksSort';
export const SET_BOOKS_FILTER = 'books/setBooksFilter';
export const SET_BOOKS_TABLE_OPTION = 'books/setBooksTableOption';
export const SET_BOOK_VALUE = 'books/setBookValue';
export const SAVE_BOOK = 'books/saveBook';
export const DELETE_BOOK = 'books/deleteBook';
export const DELETE_AUTHOR_BOOKS = 'books/deleteAuthorBooks';
export const CLEAR_BOOKS = 'books/clearBooks';
export const TOGGLE_BOOK_MONITORED = 'books/toggleBookMonitored';
export const TOGGLE_BOOKS_MONITORED = 'books/toggleBooksMonitored';

//
// Actions

export const fetchBooks = createThunk(FETCH_BOOKS);
export const fetchBooksByAuthor = createThunk(FETCH_BOOKS_BY_AUTHOR);
export const fetchBooksByIds = createThunk(FETCH_BOOKS_BY_IDS);
export const fetchBookBySlug = createThunk(FETCH_BOOK_BY_SLUG);
export const fetchBooksSchema = createThunk(FETCH_BOOKS_SCHEMA);
export const gotoBooksFirstPage = createThunk(GOTO_BOOKS_FIRST_PAGE);
export const gotoBooksPreviousPage = createThunk(GOTO_BOOKS_PREVIOUS_PAGE);
export const gotoBooksNextPage = createThunk(GOTO_BOOKS_NEXT_PAGE);
export const gotoBooksLastPage = createThunk(GOTO_BOOKS_LAST_PAGE);
export const gotoBooksPage = createThunk(GOTO_BOOKS_PAGE);
export const fetchBooksNextPage = createThunk(FETCH_BOOKS_NEXT_PAGE);
export const setBooksSort = createAction(SET_BOOKS_SORT);
export const setBooksFilter = createAction(SET_BOOKS_FILTER);
export const setBooksTableOption = createAction(SET_BOOKS_TABLE_OPTION);
export const setBookValue = createAction(SET_BOOK_VALUE);
export const saveBook = createThunk(SAVE_BOOK);
export const deleteBook = createThunk(DELETE_BOOK);
export const deleteAuthorBooks = createThunk(DELETE_AUTHOR_BOOKS);
export const clearBooks = createAction(CLEAR_BOOKS);
export const toggleBookMonitored = createThunk(TOGGLE_BOOK_MONITORED);
export const toggleBooksMonitored = createThunk(TOGGLE_BOOKS_MONITORED);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_BOOKS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const booksState = getState().books;
    const bookIndexState = getState().bookIndex;
    const defaultPayload = {
      pageSize: booksState.pageSize || 50,
      page: booksState.page || 1,
      sortKey: bookIndexState.sortKey || 'releaseDate',
      sortDirection: bookIndexState.sortDirection || sortDirections.DESCENDING
    };

    const requestPayload = { ...defaultPayload, ...payload };

    const { request, abortRequest } = createAjaxRequest({
      url: '/book',
      data: requestPayload,
      traditional: true
    });

    request.done((data) => {
      const booksData = data.records || data;

      const totalPages = data.totalPages || Math.ceil((data.totalRecords || booksData.length) / (data.pageSize || requestPayload.pageSize));

      dispatch(batchActions([
        update({ section, data: booksData }),
        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
          pageSize: data.pageSize || requestPayload.pageSize,
          totalPages,
          totalRecords: data.totalRecords || booksData.length,
          page: requestPayload.page || 1
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [FETCH_BOOKS_BY_AUTHOR]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const requestPayload = {
      authorId: payload.authorId
    };

    const { request, abortRequest } = createAjaxRequest({
      url: '/book',
      data: requestPayload,
      traditional: true
    });

    request.done((data) => {
      const booksData = data.records || data;

      const oldBooks = getState().books.items;
      const newBooks = oldBooks.filter((x) => x.authorId !== payload.authorId);
      const updatedBooksData = newBooks.concat(booksData);

      dispatch(batchActions([
        update({ section, data: updatedBooksData }),
        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
          pageSize: undefined,
          totalPages: undefined,
          totalRecords: undefined,
          page: undefined
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [FETCH_BOOKS_BY_IDS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const requestPayload = {
      bookIds: payload.bookIds
    };

    const { request, abortRequest } = createAjaxRequest({
      url: '/book',
      data: requestPayload,
      traditional: true
    });

    request.done((data) => {
      const booksData = data.records || data;

      const oldBooks = getState().books.items;
      const newBookIds = booksData.map((book) => book.id);
      const booksToKeep = oldBooks.filter((book) => !newBookIds.includes(book.id));
      const updatedBooksData = booksToKeep.concat(booksData);

      dispatch(batchActions([
        update({ section, data: updatedBooksData }),
        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
          pageSize: undefined,
          totalPages: undefined,
          totalRecords: undefined,
          page: undefined
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [FETCH_BOOK_BY_SLUG]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const requestPayload = {
      titleSlug: payload.titleSlug
    };

    const { request, abortRequest } = createAjaxRequest({
      url: '/book',
      data: requestPayload,
      traditional: true
    });

    request.done((data) => {
      const booksData = data.records || data;

      const oldBooks = getState().books.items;
      const newBookIds = booksData.map((book) => book.id);
      const booksToKeep = oldBooks.filter((book) => !newBookIds.includes(book.id));
      const updatedBooksData = booksToKeep.concat(booksData);

      dispatch(batchActions([
        update({ section, data: updatedBooksData }),
        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
          pageSize: undefined,
          totalPages: undefined,
          totalRecords: undefined,
          page: undefined
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [SAVE_BOOK]: createSaveProviderHandler(section, '/book'),
  [DELETE_BOOK]: createRemoveItemHandler(section, '/book'),

  [DELETE_AUTHOR_BOOKS]: function(getState, payload, dispatch) {
    const { authorId } = payload;
    const books = getState().books.items;

    const toDelete = books.filter((x) => x.authorId === authorId);

    dispatch(batchActions(toDelete.map((b) => removeItem({ section, id: b.id }))));
  },

  [TOGGLE_BOOK_MONITORED]: function(getState, payload, dispatch) {
    const {
      bookId,
      bookEntity = bookEntities.BOOKS,
      monitored
    } = payload;

    const bookSection = _.last(bookEntity.split('.'));

    dispatch(updateItem({
      id: bookId,
      section: bookSection,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/book/${bookId}`,
      method: 'PUT',
      data: JSON.stringify({ monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id: bookId,
        section: bookSection,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id: bookId,
        section: bookSection,
        isSaving: false
      }));
    });
  },

  [TOGGLE_BOOKS_MONITORED]: function(getState, payload, dispatch) {
    const {
      bookIds,
      bookEntity = bookEntities.BOOKS,
      monitored
    } = payload;

    dispatch(batchActions(
      bookIds.map((bookId) => {
        return updateItem({
          id: bookId,
          section: bookEntity,
          isSaving: true
        });
      })
    ));

    const promise = createAjaxRequest({
      url: '/book/monitor',
      method: 'PUT',
      data: JSON.stringify({ bookIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions(
        bookIds.map((bookId) => {
          return updateItem({
            id: bookId,
            section: bookEntity,
            isSaving: false,
            monitored
          });
        })
      ));
    });

    promise.fail((xhr) => {
      dispatch(batchActions(
        bookIds.map((bookId) => {
          return updateItem({
            id: bookId,
            section: bookEntity,
            isSaving: false
          });
        })
      ));
    });
  },

  [GOTO_BOOKS_FIRST_PAGE]: function(getState, payload, dispatch) {
    const currentPage = getState().books.page || 1;
    const nextPage = 1;

    if (currentPage !== nextPage) {
      dispatch(fetchBooks({ ...payload, page: nextPage }));
    }
  },

  [GOTO_BOOKS_PREVIOUS_PAGE]: function(getState, payload, dispatch) {
    const currentPage = getState().books.page || 1;
    const nextPage = Math.max(1, currentPage - 1);

    if (currentPage !== nextPage) {
      dispatch(fetchBooks({ ...payload, page: nextPage }));
    }
  },

  [GOTO_BOOKS_NEXT_PAGE]: function(getState, payload, dispatch) {
    const currentPage = getState().books.page || 1;
    const totalPages = getState().books.totalPages || 1;
    const nextPage = Math.min(totalPages, currentPage + 1);

    if (currentPage !== nextPage) {
      dispatch(fetchBooks({ ...payload, page: nextPage }));
    }
  },

  [GOTO_BOOKS_LAST_PAGE]: function(getState, payload, dispatch) {
    const currentPage = getState().books.page || 1;
    const totalPages = getState().books.totalPages || 1;
    const nextPage = totalPages;

    if (currentPage !== nextPage) {
      dispatch(fetchBooks({ ...payload, page: nextPage }));
    }
  },

  [GOTO_BOOKS_PAGE]: function(getState, payload, dispatch) {
    const { page } = payload;
    const currentPage = getState().books.page || 1;

    if (currentPage !== page) {
      dispatch(fetchBooks({ ...payload, page }));
    }
  },

  [FETCH_BOOKS_NEXT_PAGE]: function(getState, payload, dispatch) {
    const state = getState().books;
    const currentPage = state.page || 1;
    const totalPages = state.totalPages || 1;

    if (currentPage >= totalPages) {
      return;
    }

    const nextPage = currentPage + 1;

    dispatch(set({ section, isFetching: true }));

    const bookIndexState = getState().bookIndex;
    const defaultPayload = {
      pageSize: state.pageSize || 50,
      page: nextPage,
      sortKey: bookIndexState.sortKey || 'releaseDate',
      sortDirection: bookIndexState.sortDirection || sortDirections.DESCENDING
    };

    const requestPayload = { ...defaultPayload, ...payload };

    const { request, abortRequest } = createAjaxRequest({
      url: '/book',
      data: requestPayload,
      traditional: true
    });

    request.done((data) => {
      const booksData = data.records || data;

      const existingBooks = getState().books.items;
      const updatedBooksData = existingBooks.concat(booksData);

      dispatch(batchActions([
        update({ section, data: updatedBooksData }),
        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
          pageSize: data.pageSize || requestPayload.pageSize,
          totalPages: data.totalPages || Math.ceil((data.totalRecords || updatedBooksData.length) / (data.pageSize || requestPayload.pageSize)),
          totalRecords: data.totalRecords || updatedBooksData.length,
          page: nextPage
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  }
});

//
// Reducers

export const reducers = createHandleActions({
  [SET_BOOKS_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_BOOKS_FILTER]: createSetClientSideCollectionFilterReducer(section),

  [SET_BOOKS_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_BOOK_VALUE]: createSetSettingValueReducer(section),

  [CLEAR_BOOKS]: createClearReducer(section, {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: [],
    totalPages: 0,
    totalRecords: 0
  })
}, defaultState, section);
