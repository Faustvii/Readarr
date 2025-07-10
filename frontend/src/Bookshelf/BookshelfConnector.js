import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchBooks, fetchBooksNextPage } from 'Store/Actions/bookActions';
import { saveBookshelf, setBookshelfFilter, setBookshelfSort } from 'Store/Actions/bookshelfActions';
import createAuthorClientSideCollectionItemsSelector from 'Store/Selectors/createAuthorClientSideCollectionItemsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import Bookshelf from './Bookshelf';

function createBookFetchStateSelector() {
  return createSelector(
    (state) => state.books,
    (booksState) => {
      const bookCount = (!booksState.isFetching && booksState.isPopulated) ? booksState.items.length : 0;
      return {
        bookCount,
        isFetching: booksState.isFetching,
        isPopulated: booksState.isPopulated,
        page: booksState.page,
        totalPages: booksState.totalPages,
        totalRecords: booksState.totalRecords,
        pageSize: booksState.pageSize
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createBookFetchStateSelector(),
    createAuthorClientSideCollectionItemsSelector('bookshelf'),
    createDimensionsSelector(),
    (books, author, dimensionsState) => {
      const isPopulated = books.isPopulated && author.isPopulated;
      const isFetching = author.isFetching || books.isFetching;
      return {
        ...author,
        isPopulated,
        isFetching,
        bookCount: books.bookCount,
        isBooksPopulated: books.isPopulated,
        isSmallScreen: dimensionsState.isSmallScreen,
        currentPage: books.page,
        totalPages: books.totalPages,
        totalRecords: books.totalRecords
      };
    }
  );
}

const mapDispatchToProps = {
  setBookshelfSort,
  setBookshelfFilter,
  saveBookshelf,
  fetchBooks,
  fetchBooksNextPage
};

class BookshelfConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const { bookCount, currentPage, totalPages, totalRecords, isFetching } = this.props;
    if (
      bookCount < totalRecords &&
      currentPage &&
      totalPages &&
      currentPage < totalPages &&
      !isFetching
    ) {
      this.props.fetchBooksNextPage();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
  }

  //
  // Control

  populate = () => {
    const { bookCount, totalRecords } = this.props;
    if (bookCount === 0) {
      this.props.fetchBooks();
    } else if (totalRecords > 0 && bookCount < totalRecords) {
      this.props.fetchBooksNextPage();
    }
  };

  //
  // Listeners

  onSortPress = (sortKey) => {
    this.props.setBookshelfSort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey) => {
    this.props.setBookshelfFilter({ selectedFilterKey });
  };

  onUpdateSelectedPress = (payload) => {
    this.props.saveBookshelf(payload);
  };

  //
  // Render

  render() {
    return (
      <Bookshelf
        {...this.props}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onUpdateSelectedPress={this.onUpdateSelectedPress}
      />
    );
  }
}

BookshelfConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  setBookshelfSort: PropTypes.func.isRequired,
  setBookshelfFilter: PropTypes.func.isRequired,
  saveBookshelf: PropTypes.func.isRequired,
  fetchBooks: PropTypes.func.isRequired,
  fetchBooksNextPage: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  currentPage: PropTypes.number,
  totalPages: PropTypes.number,
  pageSize: PropTypes.number,
  totalRecords: PropTypes.number,
  isPopulated: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  bookCount: PropTypes.number,
  isBooksPopulated: PropTypes.bool
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookshelfConnector);
