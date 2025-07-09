/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { clearBooks, fetchBooks, fetchBooksNextPage, gotoBooksFirstPage, gotoBooksLastPage, gotoBooksNextPage, gotoBooksPage, gotoBooksPreviousPage } from 'Store/Actions/bookActions';
import { saveBookEditor, setBookFilter, setBookSort, setBookTableOption, setBookView } from 'Store/Actions/bookIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createBookClientSideCollectionItemsSelector from 'Store/Selectors/createBookClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import BookIndex from './BookIndex';

function createMapStateToProps() {
  return createSelector(
    createBookClientSideCollectionItemsSelector('bookIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_AUTHOR),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_BOOK),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.CUTOFF_UNMET_BOOK_SEARCH),
    createCommandExecutingSelector(commandNames.MISSING_BOOK_SEARCH),
    createDimensionsSelector(),
    (state) => state.bookIndex,
    (book, isRefreshingAuthorCommand, isRefreshingBookCommand, isRssSyncExecuting, isCutoffBooksSearch, isMissingBooksSearch, dimensionsState, bookIndex) => {
      const isRefreshingBook = isRefreshingBookCommand || isRefreshingAuthorCommand;
      return {
        ...book,
        bookIndex,
        isRefreshingBook,
        isRssSyncExecuting,
        isSearching: isCutoffBooksSearch || isMissingBooksSearch,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setBookTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setBookSort({ sortKey }));
      dispatch(clearBooks());
      dispatch(fetchBooks({ sortKey, page: 1 }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setBookFilter({ selectedFilterKey }));
    },

    dispatchSetBookView(view) {
      dispatch(setBookView({ view }));
    },

    dispatchSaveBookEditor(payload) {
      dispatch(saveBookEditor(payload));
    },

    onRefreshBookPress(items) {
      dispatch(executeCommand({
        name: commandNames.BULK_REFRESH_BOOK,
        bookIds: items
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    },

    onSearchPress(items) {
      dispatch(executeCommand({
        name: commandNames.BOOK_SEARCH,
        bookIds: items
      }));
    },

    dispatchFetchBooks() {
      dispatch(fetchBooks());
    },

    onFetchBooksNextPage() {
      dispatch(fetchBooksNextPage());
    },

    dispatchClearBooks() {
      dispatch(clearBooks());
    },

    onFirstPagePress() {
      dispatch(gotoBooksFirstPage());
    },

    onPreviousPagePress() {
      dispatch(gotoBooksPreviousPage());
    },

    onNextPagePress() {
      dispatch(gotoBooksNextPage());
    },

    onLastPagePress() {
      dispatch(gotoBooksLastPage());
    },

    onPageSelect(page) {
      dispatch(gotoBooksPage({ page }));
    }
  };
}

class BookIndexConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.populate);
    const { items, isPopulated } = this.props;
    if (!isPopulated || !items || items.length === 0) {
      this.populate();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
  }

  //
  // Control

  populate = () => {
    this.props.dispatchFetchBooks();
  };

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetBookView(view);
  };

  onSaveSelected = (payload) => {
    this.props.dispatchSaveBookEditor(payload);
  };

  onRefreshBookPress = (items) => {
    this.props.onRefreshBookPress(items);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.bookIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <BookIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onSaveSelected={this.onSaveSelected}
        onRefreshBookPress={this.onRefreshBookPress}
        onScroll={this.onScroll}
        onSortSelect={this.props.onSortSelect}
        onFetchBooksNextPage={this.props.onFetchBooksNextPage}
        onFirstPagePress={this.props.onFirstPagePress}
        onPreviousPagePress={this.props.onPreviousPagePress}
        onNextPagePress={this.props.onNextPagePress}
        onLastPagePress={this.props.onLastPagePress}
        onPageSelect={this.props.onPageSelect}
      />
    );
  }
}

BookIndexConnector.propTypes = {
  dispatchSetBookView: PropTypes.func.isRequired,
  dispatchSaveBookEditor: PropTypes.func.isRequired,
  dispatchFetchBooks: PropTypes.func.isRequired,
  onRefreshBookPress: PropTypes.func,
  onSortSelect: PropTypes.func,
  onFetchBooksNextPage: PropTypes.func,
  onFirstPagePress: PropTypes.func,
  onPreviousPagePress: PropTypes.func,
  onNextPagePress: PropTypes.func,
  onLastPagePress: PropTypes.func,
  onPageSelect: PropTypes.func,
  page: PropTypes.number,
  totalPages: PropTypes.number,
  items: PropTypes.arrayOf(PropTypes.object),
  isPopulated: PropTypes.bool,
  isFetching: PropTypes.bool
};

export default withScrollPosition(connect(createMapStateToProps, createMapDispatchToProps)(BookIndexConnector), 'bookIndex');

